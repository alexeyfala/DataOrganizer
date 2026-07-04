using DataOrganizer.Interfaces.Explorer;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace DataOrganizer.Services.Explorer;

public sealed partial class LinuxExplorerManager : ILinuxExplorerManager
{
	#region Data
	/// <summary>
	/// X11 predefined atom <c>XA_CARDINAL</c> (= 6).
	/// </summary>
	private const IntPtr AtomCardinal = 6;

	/// <summary>
	/// X11 predefined atom <c>XA_STRING</c> (= 31).
	/// </summary>
	private const IntPtr AtomString = 31;

	/// <summary>
	/// X11 predefined atom <c>XA_WINDOW</c> (= 33).
	/// </summary>
	private const IntPtr AtomWindow = 33;

	/// <summary>
	/// X11 predefined atom <c>XA_WM_NAME</c> (= 39).
	/// </summary>
	private const IntPtr AtomWmName = 39;

	/// <summary>
	/// X11 <c>ClientMessage</c> event type.
	/// </summary>
	private const int ClientMessage = 33;

	/// <summary>
	/// X11 <c>SubstructureNotifyMask | SubstructureRedirectMask</c>.
	/// </summary>
	private const long EventMask = (1L << 19) | (1L << 20);

	/// <summary>
	/// Time budget for a single D-Bus reveal call before it is abandoned.
	/// </summary>
	private const int FileManager1CallTimeoutMs = 5000;

	/// <summary>
	/// Freedesktop <c>org.freedesktop.FileManager1.ShowItems</c> method — opens a file manager
	/// with the given items selected.
	/// </summary>
	private const string FileManager1Method = "org.freedesktop.FileManager1.ShowItems";

	/// <summary>
	/// Freedesktop <c>FileManager1</c> object path.
	/// </summary>
	private const string FileManager1ObjectPath = "/org/freedesktop/FileManager1";

	/// <summary>
	/// Freedesktop <c>FileManager1</c> bus name.
	/// </summary>
	private const string FileManager1Service = "org.freedesktop.FileManager1";

	/// <summary>
	/// "libX11.so.6"
	/// </summary>
	private const string LibX11 = "libX11.so.6";

	/// <summary>
	/// EWMH <c>_NET_ACTIVE_WINDOW</c> source indicator: pager / external tool.
	/// </summary>
	private const long NetActiveWindowSourcePager = 2;

	/// <summary>
	/// Known file-manager process names compared with <c>/proc/&lt;pid&gt;/comm</c>..
	/// </summary>
	private static readonly string[] _knownFileManagerComms =
	[
		// Fly Files.
		"fly-fm-service",
		"fly-fm",

		// Nautilus (GNOME Files) — Ubuntu, Debian, Fedora and most GNOME-based distros.
		"nautilus",
		"nautilus-bin",
		"Files",

		// Dolphin — KDE Plasma (Kubuntu, KDE neon, openSUSE KDE, Manjaro KDE).
		"dolphin",

		// Nemo — Cinnamon (Linux Mint).
		"nemo",
		"nemo-desktop",

		// Thunar — XFCE (Xubuntu, Manjaro XFCE, MX Linux).
		"thunar",
		"Thunar",

		// Caja — MATE (Ubuntu MATE, Linux Mint MATE).
		"caja",

		// PCManFM-Qt — LXQt (Lubuntu).
		"pcmanfm-qt",

		// PCManFM — LXDE (legacy Lubuntu, Raspberry Pi OS).
		"pcmanfm",

		// Krusader — niche twin-panel KDE manager.
		"krusader"
	];
	#endregion

	#region Methods
	/// <inheritdoc />
	public bool TryForegroundFolder(string folderPath)
	{
		IntPtr display = IntPtr.Zero;

		try
		{
			if (string.IsNullOrEmpty(folderPath))
			{
				return false;
			}

			string folderName = Path.GetFileName(Path
				.GetFullPath(folderPath)
				.TrimEnd('/'));

			if (string.IsNullOrEmpty(folderName))
			{
				return false;
			}

			display = XOpenDisplay(IntPtr.Zero);

			if (display == IntPtr.Zero)
			{
				// No X display available (Wayland-only session, headless, etc.).
				return false;
			}

			IntPtr root = XDefaultRootWindow(display);

			IntPtr atomClientList = XInternAtom(display, "_NET_CLIENT_LIST", false);

			IntPtr atomActiveWindow = XInternAtom(display, "_NET_ACTIVE_WINDOW", false);

			IntPtr atomNetWmName = XInternAtom(display, "_NET_WM_NAME", false);

			IntPtr atomNetWmPid = XInternAtom(display, "_NET_WM_PID", false);

			IntPtr atomUtf8 = XInternAtom(display, "UTF8_STRING", false);

			if (!TryGetWindowList(
				display,
				root,
				atomClientList,
				out IntPtr[] windows))
			{
				return false;
			}

			foreach (IntPtr window in windows)
			{
				if (!TryGetWindowPid(
					display,
					window,
					atomNetWmPid,
					out int pid))
				{
					continue;
				}

				if (!IsKnownFileManagerProcess(pid))
				{
					continue;
				}

				string? title = GetWindowTitle(
					display,
					window,
					atomNetWmName,
					atomUtf8);

				if (string.IsNullOrEmpty(title))
				{
					continue;
				}

				if (!TitleMatchesFolderName(title, folderName))
				{
					continue;
				}

				if (TryActivateWindow(
					display,
					root,
					window,
					atomActiveWindow))
				{
					_ = XFlush(display);

					return true;
				}
			}

			return false;
		}
		finally
		{
			if (display != IntPtr.Zero)
			{
				try
				{
					_ = XCloseDisplay(display);
				}
				catch
				{
				}
			}
		}
	}

	/// <inheritdoc />
	public bool TryRevealFile(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return false;
		}

		string uri = new Uri(Path.GetFullPath(filePath)).AbsoluteUri;

		// Primary transport: gdbus (GLib) — present on GTK/GNOME-based desktops.
		if (TryCallShowItems(
			"gdbus",
			[
				"call",
					"--session",
					"--dest", FileManager1Service,
					"--object-path", FileManager1ObjectPath,
					"--method", FileManager1Method,
					$"['{uri}']",
					""
			]))
		{
			return true;
		}

		// Fallback transport: dbus-send — shipped with the dbus package on virtually every desktop.
		return TryCallShowItems(
			"dbus-send",
			[
				"--session",
					$"--dest={FileManager1Service}",
					"--type=method_call",
					FileManager1ObjectPath,
					FileManager1Method,
					$"array:string:{uri}",
					"string:"
			]);
	}
	#endregion

	#region Service
	/// <summary>
	/// Reads window title — prefers UTF-8 <c>_NET_WM_NAME</c>, falls back to <c>WM_NAME</c>.
	/// </summary>
	private static string? GetWindowTitle(
		IntPtr display,
		IntPtr window,
		IntPtr atomNetWmName,
		IntPtr atomUtf8)
	{
		string? title = ReadStringProperty(
			display,
			window,
			atomNetWmName,
			atomUtf8);

		if (!string.IsNullOrEmpty(title))
		{
			return title;
		}

		return ReadStringProperty(
			display,
			window,
			AtomWmName,
			AtomString);
	}

	/// <summary>
	/// Checks whether the process owning the window is a known file manager
	/// (filters out unrelated apps that might happen to have matching titles).
	/// </summary>
	private static bool IsKnownFileManagerProcess(int pid)
	{
		try
		{
			string commPath = $"/proc/{pid}/comm";

			if (!File.Exists(commPath))
			{
				return false;
			}

			string comm = File
				.ReadAllText(commPath)
				.Trim();

			foreach (string knownComm in _knownFileManagerComms)
			{
				if (comm.Equals(knownComm, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Reads a string-typed window property and decodes it as UTF-8.
	/// </summary>
	private static string? ReadStringProperty(
		IntPtr display,
		IntPtr window,
		IntPtr property,
		IntPtr reqType)
	{
		IntPtr prop = IntPtr.Zero;

		try
		{
			int status = XGetWindowProperty(
				display,
				window,
				property,
				IntPtr.Zero,
				0xFFFFFF,
				false,
				reqType,
				out _,
				out _,
				out IntPtr nitems,
				out _,
				out prop);

			if (status != 0
				|| prop == IntPtr.Zero
				|| (long)nitems == 0)
			{
				return null;
			}

			int byteCount = (int)nitems;

			byte[] bytes = new byte[byteCount];

			Marshal.Copy(
				prop,
				bytes,
				0,
				byteCount);

			return Encoding
				.UTF8
				.GetString(bytes);
		}
		finally
		{
			if (prop != IntPtr.Zero)
			{
				_ = XFree(prop);
			}
		}
	}

	/// <summary>
	/// Decides whether a window title belongs to the requested folder.
	/// </summary>
	private static bool TitleMatchesFolderName(string title, string folderName)
	{
		if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(folderName))
		{
			return false;
		}

		if (title.Equals(folderName, StringComparison.Ordinal))
		{
			return true;
		}

		// Title starts with folder name followed by a space — typical "<name> - <suffix>" format.
		return title.Length > folderName.Length
			&& title.AsSpan(0, folderName.Length).Equals(folderName.AsSpan(), StringComparison.Ordinal)
			&& title[folderName.Length] == ' ';
	}

	/// <summary>
	/// Sends <c>_NET_ACTIVE_WINDOW</c> ClientMessage to the root window — the EWMH-standard way
	/// to ask the window manager to raise, focus, and de-iconify the target window.
	/// </summary>
	private static bool TryActivateWindow(
		IntPtr display,
		IntPtr root,
		IntPtr window,
		IntPtr atomActiveWindow)
	{
		XClientMessageEvent ev = new()
		{
			type = ClientMessage,
			serial = IntPtr.Zero,
			send_event = 1,
			display = display,
			window = window,
			message_type = atomActiveWindow,
			format = 32,
			data0 = new IntPtr(NetActiveWindowSourcePager),
			data1 = IntPtr.Zero,
			data2 = IntPtr.Zero,
			data3 = IntPtr.Zero,
			data4 = IntPtr.Zero
		};

		int status = XSendEvent(
			display,
			root,
			false,
			new IntPtr(EventMask),
			ref ev);

		_ = XRaiseWindow(display, window);

		return status != 0;
	}

	/// <summary>
	/// Invokes <c>FileManager1.ShowItems</c> through the given D-Bus command-line tool;
	/// returns whether the call completed successfully.
	/// </summary>
	private static bool TryCallShowItems(string executable, string[] arguments)
	{
		try
		{
			ProcessStartInfo startInfo = new()
			{
				FileName = executable,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			foreach (string argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}

			using Process? process = Process.Start(startInfo);

			if (process is null)
			{
				return false;
			}

			// The call returns once the file manager accepts the request; abandon a missing or stuck tool.
			if (!process.WaitForExit(FileManager1CallTimeoutMs))
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch
				{
				}

				return false;
			}

			return process.ExitCode == 0;
		}
		catch
		{
			// Tool not installed or failed to launch — the caller falls back to the next transport.
			return false;
		}
	}

	/// <summary>
	/// Reads <c>_NET_CLIENT_LIST</c> from the root window — array of all top-level windows
	/// known to the EWMH-compliant window manager.
	/// </summary>
	private static bool TryGetWindowList(
		IntPtr display,
		IntPtr root,
		IntPtr property,
		out IntPtr[] windows)
	{
		windows = [];

		IntPtr prop = IntPtr.Zero;

		try
		{
			int status = XGetWindowProperty(
				display,
				root,
				property,
				IntPtr.Zero,
				0xFFFFFF,
				false,
				AtomWindow,
				out _,
				out int actualFormat,
				out IntPtr nitems,
				out _,
				out prop);

			if (status != 0 || prop == IntPtr.Zero || actualFormat != 32)
			{
				return false;
			}

			int count = (int)(long)nitems;

			windows = new IntPtr[count];

			// X11 returns 32-bit-format properties as native long array
			// (8 bytes per entry on 64-bit Linux, 4 bytes on 32-bit).
			for (int i = 0; i < count; i++)
			{
				windows[i] = Marshal.ReadIntPtr(prop, i * IntPtr.Size);
			}

			return true;
		}
		finally
		{
			if (prop != IntPtr.Zero)
			{
				_ = XFree(prop);
			}
		}
	}

	/// <summary>
	/// Reads <c>_NET_WM_PID</c> for the given window.
	/// </summary>
	private static bool TryGetWindowPid(
		IntPtr display,
		IntPtr window,
		IntPtr property,
		out int pid)
	{
		pid = 0;

		IntPtr prop = IntPtr.Zero;

		try
		{
			int status = XGetWindowProperty(
				display,
				window,
				property,
				IntPtr.Zero,
				1,
				false,
				AtomCardinal,
				out _,
				out int actualFormat,
				out IntPtr nitems,
				out _,
				out prop);

			if (status != 0 || prop == IntPtr.Zero || actualFormat != 32 || (long)nitems < 1)
			{
				return false;
			}

			IntPtr value = Marshal.ReadIntPtr(prop, 0);

			pid = (int)value.ToInt64();

			return pid > 0;
		}
		finally
		{
			if (prop != IntPtr.Zero)
			{
				_ = XFree(prop);
			}
		}
	}
	#endregion

	#region Native
	[LibraryImport(LibX11)]
	private static partial int XCloseDisplay(IntPtr display);

	[LibraryImport(LibX11)]
	private static partial IntPtr XDefaultRootWindow(IntPtr display);

	[LibraryImport(LibX11)]
	private static partial int XFlush(IntPtr display);

	[LibraryImport(LibX11)]
	private static partial int XFree(IntPtr data);

	[LibraryImport(LibX11)]
	private static partial int XGetWindowProperty(
		IntPtr display,
		IntPtr window,
		IntPtr property,
		IntPtr longOffset,
		IntPtr longLength,
		[MarshalAs(UnmanagedType.Bool)] bool delete,
		IntPtr reqType,
		out IntPtr actualType,
		out int actualFormat,
		out IntPtr nitems,
		out IntPtr bytesAfter,
		out IntPtr prop);

	[LibraryImport(LibX11, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
	private static partial IntPtr XInternAtom(
		IntPtr display,
		string atomName,
		[MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

	[LibraryImport(LibX11)]
	private static partial IntPtr XOpenDisplay(IntPtr displayName);

	[LibraryImport(LibX11)]
	private static partial int XRaiseWindow(IntPtr display, IntPtr window);

	[LibraryImport(LibX11)]
	private static partial int XSendEvent(
		IntPtr display,
		IntPtr window,
		[MarshalAs(UnmanagedType.Bool)] bool propagate,
		IntPtr eventMask,
		ref XClientMessageEvent eventSend);

	[StructLayout(LayoutKind.Sequential)]
	private struct XClientMessageEvent
	{
		public int type;

		public IntPtr serial;

		public int send_event;

		public IntPtr display;

		public IntPtr window;

		public IntPtr message_type;

		public int format;

		public IntPtr data0;

		public IntPtr data1;

		public IntPtr data2;

		public IntPtr data3;

		public IntPtr data4;
	}
	#endregion
}
