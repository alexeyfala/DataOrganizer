using Avalonia.Media;
using Avalonia.Media.Imaging;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

/// <summary>
/// Windows-only implementation of <see cref="IAppPickerService" />. Enumerates
/// candidate applications for a file via shell32 <c>SHAssocEnumHandlers</c>, extracts
/// icons via <c>SHGetFileInfo</c>, and delegates the UI to <see cref="IDialogService" />.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsAppPickerService : IAppPickerService
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

		AssociatedAppInfo[] candidates = EnumerateCandidates(extension);

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
	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DestroyIcon(IntPtr hIcon);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHAssocEnumHandlers(
			[MarshalAs(UnmanagedType.LPWStr)] string? pszExtra,
			AssocFilter afFilter,
			[MarshalAs(UnmanagedType.Interface)] out IEnumAssocHandlers? ppEnumHandler);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")]
	private static extern IntPtr SHGetFileInfo(
			string pszPath,
			uint dwFileAttributes,
			ref SHFILEINFO psfi,
			uint cbFileInfo,
			uint uFlags);

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

		IImage? icon = TryLoadIcon(appPath);

		return new AssociatedAppInfo
		{
			AppName = uiName,
			AppPath = appPath,
			Icon = icon
		};
	}

	/// <summary>
	/// Extracts the large shell icon for <paramref name="appPath" /> via
	/// <c>SHGetFileInfo</c>, converts it through PNG and returns it as an Avalonia
	/// <see cref="Bitmap" />. Returns <c>null</c> on any failure — the picker can still
	/// list the app without an icon.
	/// </summary>
	private static IImage? TryLoadIcon(string appPath)
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

			using System.Drawing.Bitmap gdiBitmap = icon.ToBitmap();

			using MemoryStream memoryStream = new();

			gdiBitmap.Save(memoryStream, ImageFormat.Png);

			memoryStream.Position = 0;

			return new Avalonia.Media.Imaging.Bitmap(memoryStream);
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
	/// Walks <c>SHAssocEnumHandlers</c> and converts each <c>IAssocHandler</c> into an
	/// <see cref="AssociatedAppInfo" />. Bad / unreadable entries are logged and skipped
	/// so a single broken registry record does not crash the picker.
	/// </summary>
	private AssociatedAppInfo[] EnumerateCandidates(string extension)
	{
		IEnumAssocHandlers? enumerator = null;

		try
		{
			int hr = SHAssocEnumHandlers(
				string.IsNullOrEmpty(extension) ? null : extension,
				AssocFilter.None,
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
	/// File information block populated by <see cref="SHGetFileInfo" />.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct SHFILEINFO
	{
		public IntPtr hIcon;
		public int iIcon;
		public uint dwAttributes;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}
	#endregion
}
