using Avalonia.Media.Imaging;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using Microsoft.Win32;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using DrawingBitmap = System.Drawing.Bitmap;
using Icon = System.Drawing.Icon;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace DataOrganizer.Services;

/// <summary>
/// Windows-only implementation of <see cref="IAppPickerService" />. Enumerates
/// candidate applications for a file via shell32 <c>SHAssocEnumHandlers</c>, extracts
/// icons via <c>SHGetFileInfo</c>, and delegates the UI to <see cref="IDialogService" />.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class WindowsAppPickerService : IAppPickerService
{
	#region Data
	private const int S_OK = 0;

	private const uint SHGFI_ICON = 0x000000100;

	private const uint SHGFI_LARGEICON = 0x000000000;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public WindowsAppPickerService(
		IDialogService dialogService,
		ILogger logger)
	{
		_dialogService = dialogService;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<AssociatedAppInfo?> PickAppAsync(string filePath, CancellationToken token = default)
	{
		string extension = Path.GetExtension(filePath);

		AssociatedAppInfo[] candidates;

		if (string.IsNullOrEmpty(extension))
		{
			candidates = EnumerateCandidates(".txt");

			if (candidates.Length == 0)
			{
				candidates = EnumerateAllApplications();
			}
		}
		else
		{
			candidates = EnumerateCandidates(extension);
		}

		if (candidates.Length == 0)
		{
			_logger.LogWarning($@"No application handlers found for ""{filePath}""; picker not shown.");

			return null;
		}

		return await _dialogService
			.PickAppAsync(candidates, token)
			.ConfigureAwait(false);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Extracts the executable path from a Windows registry "open command" string,
	/// handling both quoted ("C:\path\app.exe" "%1") and unquoted (app.exe %1) forms.
	/// </summary>
	private static string? ExtractExecutablePath(string command)
	{
		string trimmed = command.Trim();

		if (trimmed.StartsWith('"'))
		{
			int end = trimmed.IndexOf('"', 1);

			return end < 0 ? null : trimmed[1..end];
		}

		int space = trimmed.IndexOf(' ');

		return space < 0 ? trimmed : trimmed[..space];
	}

	/// <summary>
	/// Resolves the display name of an application. <c>FriendlyAppName</c> may be a
	/// literal ("Notepad") or an indirect resource pointer
	/// ("@%SystemRoot%\system32\notepad.exe,-469"); for the latter we go through
	/// <c>SHLoadIndirectString</c>. Falls back to the registry subkey name when
	/// neither form is usable.
	/// </summary>
	private static string ResolveFriendlyName(RegistryKey appKey, string subkeyName)
	{
		if (appKey.GetValue("FriendlyAppName") is not string friendly || string.IsNullOrEmpty(friendly))
		{
			return subkeyName;
		}

		if (!friendly.StartsWith('@'))
		{
			return friendly;
		}

		char[] buffer = new char[1024];

		if (SHLoadIndirectString(friendly, buffer, buffer.Length, IntPtr.Zero) != S_OK)
		{
			return subkeyName;
		}

		int nullTerminator = Array.IndexOf(buffer, '\0');

		return new string(buffer, 0, nullTerminator >= 0 ? nullTerminator : buffer.Length);
	}

	/// <summary>
	/// Reads display name, executable path and icon from a single
	/// <see cref="IAssocHandler" />. Returns <c>null</c> when the handler is missing
	/// the executable path (rare, but possible for partially-broken registry entries).
	/// </summary>
	private static AssociatedAppInfo? TryBuildInfo(IAssocHandler handler)
	{
		int hr = handler.GetName(out string? appPath);

		if (hr != S_OK || string.IsNullOrEmpty(appPath))
		{
			return null;
		}

		hr = handler.GetUIName(out string? uiName);

		if (hr != S_OK || string.IsNullOrEmpty(uiName))
		{
			uiName = Path.GetFileNameWithoutExtension(appPath);
		}

		return new AssociatedAppInfo
		{
			AppName = uiName,
			AppPath = appPath,
			Icon = TryLoadIcon(appPath)
		};
	}

	/// <summary>
	/// Builds an <see cref="AssociatedAppInfo" /> from one <c>App Paths\&lt;exe&gt;</c>
	/// entry. The display name is the executable name without extension; the icon is
	/// loaded from the resolved exe path.
	/// </summary>
	private static AssociatedAppInfo? TryBuildInfoFromAppPaths(RegistryKey appPathsKey, string subkeyName)
	{
		using RegistryKey? appKey = appPathsKey.OpenSubKey(subkeyName);

		if (appKey?.GetValue(null) is not string appPath || string.IsNullOrEmpty(appPath))
		{
			return null;
		}

		appPath = appPath.Trim('"');

		return new AssociatedAppInfo
		{
			AppName = Path.GetFileNameWithoutExtension(subkeyName),
			AppPath = appPath,
			Icon = TryLoadIcon(appPath)
		};
	}

	/// <summary>
	/// Builds an <see cref="AssociatedAppInfo" /> from one
	/// <c>HKEY_CLASSES_ROOT\Applications\&lt;subkey&gt;</c> entry. Skips apps marked
	/// with <c>NoOpenWith</c> or missing the <c>shell\open\command</c> value.
	/// </summary>
	private static AssociatedAppInfo? TryBuildInfoFromRegistry(RegistryKey appsKey, string subkeyName)
	{
		using RegistryKey? appKey = appsKey.OpenSubKey(subkeyName);

		if (appKey is null)
		{
			return null;
		}

		if (appKey
			.GetValueNames()
			.Contains("NoOpenWith", StringComparer.OrdinalIgnoreCase))
		{
			return null;
		}

		using RegistryKey? commandKey = appKey.OpenSubKey(@"shell\open\command");

		if (commandKey?.GetValue(null) is not string command || string.IsNullOrEmpty(command))
		{
			return null;
		}

		string? executablePath = ExtractExecutablePath(command);

		if (string.IsNullOrEmpty(executablePath))
		{
			return null;
		}

		return new AssociatedAppInfo
		{
			AppName = ResolveFriendlyName(appKey, subkeyName),
			AppPath = executablePath,
			Icon = TryLoadIcon(executablePath)
		};
	}

	/// <summary>
	/// Loads the large shell icon for <paramref name="appPath" /> as an Avalonia
	/// bitmap; <c>null</c> on any failure.
	/// </summary>
	private static Bitmap? TryLoadIcon(string appPath)
	{
		SHFILEINFO info = default;

		IntPtr handle = SHGetFileInfo(
			appPath,
			0,
			ref info,
			(uint)Marshal.SizeOf<SHFILEINFO>(),
			SHGFI_ICON | SHGFI_LARGEICON);

		if (handle == IntPtr.Zero || info.hIcon == IntPtr.Zero)
		{
			return null;
		}

		try
		{
			using Icon icon = (Icon)Icon.FromHandle(info.hIcon).Clone();

			using DrawingBitmap gdiBitmap = icon.ToBitmap();

			using MemoryStream memoryStream = new();

			gdiBitmap.Save(memoryStream, ImageFormat.Png);

			memoryStream.Position = 0;

			return new Bitmap(memoryStream);
		}
		catch
		{
			return null;
		}
		finally
		{
			_ = DestroyIcon(info.hIcon);
		}
	}

	/// <summary>
	/// Adds entries from <c>SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths</c>
	/// under <paramref name="root" /> (typically <c>Registry.LocalMachine</c> for
	/// system-wide apps and <c>Registry.CurrentUser</c> for per-user apps). Each
	/// subkey is an executable name whose <c>(Default)</c> value holds the full path.
	/// </summary>
	private void AddAppPathsTo(
		List<AssociatedAppInfo> result,
		HashSet<string> seenPaths,
		RegistryKey root)
	{
		using RegistryKey? appPathsKey = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");

		if (appPathsKey is null)
		{
			return;
		}

		foreach (string subkeyName in appPathsKey.GetSubKeyNames())
		{
			try
			{
				if (TryBuildInfoFromAppPaths(appPathsKey, subkeyName) is { } info && seenPaths.Add(info.AppPath))
				{
					result.Add(info);
				}
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);
			}
		}
	}

	/// <summary>
	/// Adds entries from <c>HKEY_CLASSES_ROOT\Applications</c>, skipping apps marked
	/// with <c>NoOpenWith</c> and ones whose <see cref="AssociatedAppInfo.AppPath" />
	/// is already in <paramref name="seenPaths" />.
	/// </summary>
	private void AddHkcrApplicationsTo(List<AssociatedAppInfo> result, HashSet<string> seenPaths)
	{
		using RegistryKey? appsKey = Registry
			.ClassesRoot
			.OpenSubKey("Applications");

		if (appsKey is null)
		{
			_logger.LogWarning(
				@"Registry key HKEY_CLASSES_ROOT\Applications not found; skipping this source.");

			return;
		}

		foreach (string subkeyName in appsKey.GetSubKeyNames())
		{
			try
			{
				if (TryBuildInfoFromRegistry(appsKey, subkeyName) is { } info && seenPaths.Add(info.AppPath))
				{
					result.Add(info);
				}
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);
			}
		}
	}

	/// <summary>
	/// Aggregates applications from three registry sources: <c>HKCR\Applications</c>
	/// (third-party "Open with" handlers), <c>HKLM</c> and <c>HKCU</c>
	/// <c>App Paths</c> (built-in system apps like Notepad, Calc). Duplicates are
	/// removed by executable path (case-insensitive).
	/// </summary>
	private AssociatedAppInfo[] EnumerateAllApplications()
	{
		List<AssociatedAppInfo> result = [];

		HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);

		AddHkcrApplicationsTo(result, seenPaths);

		AddAppPathsTo(result, seenPaths, Registry.LocalMachine);

		AddAppPathsTo(result, seenPaths, Registry.CurrentUser);

		return [.. result];
	}

	/// <summary>
	/// Walks <c>SHAssocEnumHandlers</c> and converts each <c>IAssocHandler</c> into an
	/// <see cref="AssociatedAppInfo" />. Bad / unreadable entries are logged and skipped
	/// so a single broken registry record does not crash the picker.
	/// </summary>
	private AssociatedAppInfo[] EnumerateCandidates(string extension, AssocFilter filter = AssocFilter.None)
	{
		IEnumAssocHandlers? enumerator = null;

		try
		{
			int hr = SHAssocEnumHandlers(
				string.IsNullOrEmpty(extension) ? null : extension,
				filter,
				out enumerator);

			if (hr != S_OK || enumerator is null)
			{
				_logger.LogWarning($"SHAssocEnumHandlers failed for extension \"{extension}\" with HRESULT 0x{hr:X8}.");

				return [];
			}

			List<AssociatedAppInfo> result = [];

			IAssocHandler[] buffer = new IAssocHandler[1];

			while (enumerator.Next(1, buffer, out int fetched) == S_OK && fetched == 1)
			{
				IAssocHandler handler = buffer[0];

				try
				{
					if (TryBuildInfo(handler) is { } info)
					{
						result.Add(info);
					}
				}
				catch (Exception ex)
				{
					_logger.LogException(ex);
				}
				finally
				{
					Marshal.ReleaseComObject(handler);
				}
			}

			return [.. result];
		}
		finally
		{
			if (enumerator is not null)
			{
				Marshal.ReleaseComObject(enumerator);
			}
		}
	}
	#endregion

	#region Native
	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool DestroyIcon(IntPtr hIcon);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHAssocEnumHandlers(
		[MarshalAs(UnmanagedType.LPWStr)] string? pszExtra,
		AssocFilter afFilter,
		[MarshalAs(UnmanagedType.Interface)] out IEnumAssocHandlers? ppEnumHandler);

	[LibraryImport("shell32.dll", EntryPoint = "SHGetFileInfoW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial IntPtr SHGetFileInfo(
		string pszPath,
		uint dwFileAttributes,
		ref SHFILEINFO psfi,
		uint cbFileInfo,
		uint uFlags);

	[LibraryImport("shlwapi.dll", EntryPoint = "SHLoadIndirectString", StringMarshalling = StringMarshalling.Utf16)]
	private static partial int SHLoadIndirectString(
		string pszSource,
		[Out] char[] pszOutBuf,
		int cchOutBuf,
		IntPtr ppvReserved);
	#endregion

	#region Nested Types
	/// <summary>
	/// Filter flags for <see cref="SHAssocEnumHandlers" />.
	/// </summary>
	private enum AssocFilter
	{
		None = 0,
		Recommended = 1
	}

	/// <summary>
	/// COM interface <c>IAssocHandler</c> — minimal binding (display name, executable
	/// path; remaining methods are present only to preserve the v-table slot order).
	/// </summary>
	[ComImport]
	[Guid("F04061AC-1659-4A3F-A954-775AA57FC083")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAssocHandler
	{
		[PreserveSig]
		int GetName([MarshalAs(UnmanagedType.LPWStr)] out string? ppsz);

		[PreserveSig]
		int GetUIName([MarshalAs(UnmanagedType.LPWStr)] out string? ppsz);

		[PreserveSig]
		int GetIconLocation(
			[MarshalAs(UnmanagedType.LPWStr)] out string? ppszPath,
			out int pIndex);

		[PreserveSig]
		int IsRecommended();

		[PreserveSig]
		int MakeDefault([MarshalAs(UnmanagedType.LPWStr)] string pszDescription);

		[PreserveSig]
		int Invoke(IntPtr pdo);

		[PreserveSig]
		int CreateInvoker(IntPtr pdo, out IntPtr ppInvoker);
	}

	/// <summary>
	/// COM interface <c>IEnumAssocHandlers</c> — minimal binding (only the
	/// <c>Next</c> method is used).
	/// </summary>
	[ComImport]
	[Guid("973810AE-9599-4B88-9E4D-6EE98C9552DA")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IEnumAssocHandlers
	{
		[PreserveSig]
		int Next(
			int celt,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IAssocHandler[] rgelt,
			out int pceltFetched);
	}

	/// <summary>
	/// Blittable mirror of the native <c>SHFILEINFOW</c> struct; string fields use
	/// <c>fixed char[N]</c> buffers.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct SHFILEINFO
	{
		public IntPtr hIcon;

		public int iIcon;

		public uint dwAttributes;

		public fixed char szDisplayName[260];

		public fixed char szTypeName[80];
	}
	#endregion
}
