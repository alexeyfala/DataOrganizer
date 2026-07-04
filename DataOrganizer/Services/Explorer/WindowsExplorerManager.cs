using DataOrganizer.Interfaces.Explorer;
using Interop.UIAutomationClient;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DataOrganizer.Services.Explorer;

public sealed partial class WindowsExplorerManager : IWindowsExplorerManager
{
	#region Data
	/// <summary>
	/// "kernel32.dll"
	/// </summary>
	private const string Kernel32Dll = "kernel32.dll";

	/// <summary>
	/// "user32.dll"
	/// </summary>
	private const string User32Dll = "user32.dll";
	#endregion

	#region Methods
	/// <inheritdoc />
	public bool TryForegroundFolder(string folderPath, string? selectItemPath = null)
	{
		const char separator = '\\';

		folderPath = Path
			.GetFullPath(folderPath)
			.TrimEnd(separator);

		// ShellWindows CLSID: 9BA05972-F6A8-11CF-A442-00A0C90A8F39
		if (Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")) is not { } shellWindowsType)
		{
			return false;
		}

		dynamic? shellWindows = Activator.CreateInstance(shellWindowsType);

		if (shellWindows is null)
		{
			return false;
		}

		try
		{
			int count = shellWindows.Count;

			for (int i = 0; i < count; i++)
			{
				dynamic? window;

				try
				{
					window = shellWindows.Item(i);
				}
				catch
				{
					continue;
				}

				if (window is null)
				{
					continue;
				}

				try
				{
					string fileName = Path.GetFileNameWithoutExtension(window.FullName);

					if (!fileName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					if (window.LocationURL is not string url || string.IsNullOrEmpty(url))
					{
						continue;
					}

					string path = new Uri(url)
						.LocalPath
						.TrimEnd(separator);

					if (string.Equals(
						path,
						folderPath,
						StringComparison.OrdinalIgnoreCase))
					{
						IntPtr hwnd = (int)window.HWND;

						if (IsIconic(hwnd))
						{
							ShowWindow(hwnd, SW_RESTORE);
						}

						ForceForeground(hwnd);

						TrySwitchToTab(hwnd, folderPath);

						if (selectItemPath is not null)
						{
							TrySelectItem(window, selectItemPath);
						}

						return true;
					}
				}
				catch
				{
				}
			}
		}
		finally
		{
			Marshal.ReleaseComObject(shellWindows);
		}

		return false;
	}
	#endregion

	#region Service
	/// <summary>
	/// Brings the window to the front.
	/// <see cref="SetForegroundWindow" /> only works if the calling thread is - foreground.
	/// Trick: temporarily bind your thread to the thread of the current foreground window.
	/// </summary>
	private static void ForceForeground(IntPtr hwnd)
	{
		IntPtr foreground = GetForegroundWindow();

		uint foregroundThread = GetWindowThreadProcessId(foreground, out _);

		uint currentThread = GetCurrentThreadId();

		if (foregroundThread != currentThread)
		{
			AttachThreadInput(currentThread, foregroundThread, true);

			SetForegroundWindow(hwnd);

			ShowWindow(hwnd, SW_SHOW);

			AttachThreadInput(currentThread, foregroundThread, false);
		}
		else
		{
			SetForegroundWindow(hwnd);

			ShowWindow(hwnd, SW_SHOW);
		}
	}

	/// <summary>
	/// Selects the specified item inside the active tab of the Explorer window.
	/// </summary>
	private static void TrySelectItem(dynamic window, string itemPath)
	{
		try
		{
			dynamic? document = window.Document;

			if (document is null)
			{
				return;
			}

			string fileName = Path.GetFileName(itemPath);

			dynamic? folderItem = document.Folder?.ParseName(fileName);

			if (folderItem is null)
			{
				return;
			}

			document.SelectItem(folderItem, SVSI_SELECT_ITEM);
		}
		catch
		{
			// Shell automation failed — not critical, folder is already in foreground
		}
	}

	/// <summary>
	/// Switches tabs via UI Automation (Windows 11).
	/// </summary>
	private static void TrySwitchToTab(IntPtr hwnd, string folderPath)
	{
		try
		{
			CUIAutomation uia = new();

			IUIAutomationElement element = uia.ElementFromHandle(hwnd);

			// UIA_ControlTypePropertyId = 30003, UIA_TabItemControlTypeId = 50019
			IUIAutomationCondition tabCondition = uia.CreatePropertyCondition(30003, 50019);

			IUIAutomationElementArray tabs = element.FindAll(TreeScope.TreeScope_Descendants, tabCondition);

			string folderName = Path.GetFileName(folderPath);

			for (int i = 0; i < tabs.Length; i++)
			{
				IUIAutomationElement tab = tabs.GetElement(i);

				if (string.Equals(tab.CurrentName, folderName, StringComparison.OrdinalIgnoreCase))
				{
					// SelectionItemPattern ID = 10010
					try
					{
						IUIAutomationSelectionItemPattern pattern = (IUIAutomationSelectionItemPattern)tab.GetCurrentPattern(10010);

						pattern.Select();
					}
					catch
					{
						// Fallback: InvokePattern ID = 10000
						try
						{
							IUIAutomationInvokePattern invoke = (IUIAutomationInvokePattern)tab.GetCurrentPattern(10000);

							invoke.Invoke();
						}
						catch
						{
						}
					}

					break;
				}
			}
		}
		catch
		{
			// UI Automation failed — not critical, window is already in foreground
		}
	}
	#endregion

	#region Native
	private const int SVSI_SELECT_ITEM = 0x001D;
	private const int SW_RESTORE = 9;

	private const int SW_SHOW = 5;
	[LibraryImport(User32Dll)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool AttachThreadInput(
		uint idAttach,
		uint idAttachTo,
		[MarshalAs(UnmanagedType.Bool)] bool fAttach);

	[LibraryImport(Kernel32Dll)]
	private static partial uint GetCurrentThreadId();

	[LibraryImport(User32Dll)]
	private static partial IntPtr GetForegroundWindow();

	[LibraryImport(User32Dll)]
	private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[LibraryImport(User32Dll)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool IsIconic(IntPtr hWnd);

	[LibraryImport(User32Dll)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool SetForegroundWindow(IntPtr hWnd);

	[LibraryImport(User32Dll)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);
	#endregion
}
