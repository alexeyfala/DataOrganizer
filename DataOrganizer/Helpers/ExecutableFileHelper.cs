using System;
using System.Collections.Frozen;
using System.IO;

namespace DataOrganizer.Helpers;

/// <summary>
/// Detects whether a file is potentially executable based on its extension.
/// </summary>
internal static class ExecutableFileHelper
{
	#region Data
	/// <summary>
	/// Case-insensitive set of file extensions whose double-click / shell-execute
	/// action runs code on Windows, Linux or macOS. Covers direct executables,
	/// shell / interpreted scripts, installers and shortcuts.
	/// </summary>
	private static readonly FrozenSet<string> _executableExtensions = new[]
	{
	#region Windows
		".exe",         // Native executables
		".com",         // DOS-era executables
		".scr",         // Screensavers (actually executables)
		".pif",         // Program info files (executable on legacy systems)
		".cpl",         // Control Panel items
		".msi",         // Windows Installer packages
		".msp",         // Windows Installer patches
		".msu",         // Windows Update Standalone Installer
		".bat",         // Batch scripts
		".cmd",         // Command scripts
		".vbs",         // VBScript
		".vbe",         // Encoded VBScript
		".js",          // JScript (Windows Script Host, not browser-only)
		".jse",         // Encoded JScript
		".ws",          // Windows Script
		".wsf",         // Windows Script File
		".wsh",         // Windows Script Host settings
		".ps1",         // PowerShell script
		".psm1",        // PowerShell module
		".psd1",        // PowerShell data file
		".ps1xml",      // PowerShell XML config
		".hta",         // HTML Application (runs with full local privileges)
		".chm",         // Compiled HTML Help (can run code)
		".msc",         // Microsoft Management Console snap-in
		".gadget",      // Windows Desktop Gadget
		".jar",         // Java archive (runs if JRE installed)
		".reg",         // Registry file (modifies registry on double-click)
		".lnk",         // Shortcut (targets executable)
		".url",         // Internet shortcut
	#endregion

	#region Linux / Unix
		".sh",          // Shell scripts (POSIX sh)
		".bash",        // Bash scripts
		".zsh",         // Zsh scripts
		".ksh",         // Korn shell scripts
		".csh",         // C shell scripts
		".fish",        // Fish shell scripts
		".run",         // Generic self-extracting Linux installers
		".appimage",    // AppImage portable executables
		".flatpakref",  // Flatpak reference (triggers install)
		".flatpak",     // Flatpak bundles
		".snap",        // Snap packages
		".deb",         // Debian packages (installer prompts on open)
		".rpm",         // RPM packages
		".desktop",     // Desktop entries (Exec= field runs arbitrary command)
	#endregion

	#region macOS
		".app",         // macOS application bundles
		".command",     // Executable shell scripts launched by Terminal
		".tool",        // Terminal tool scripts
		".dmg",         // Disk images (often contain installers)
		".pkg",         // macOS installer packages
		".mpkg",        // macOS meta installer packages
		".scpt",        // Compiled AppleScript
		".scptd",       // AppleScript bundles
		".applescript", // AppleScript source
		".action",      // Automator actions
		".workflow",    // Automator workflows
		".osax",        // Scripting Additions
	#endregion

	#region Cross-platform interpreted scripts
		".py",          // Python
		".pyw",         // Python (no-console on Windows)
		".pl",          // Perl
		".rb",          // Ruby
		".lua",         // Lua
		".php",         // PHP CLI scripts
		".tcl",         // Tcl scripts
	#endregion		
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region Methods
	/// <summary>
	/// <c>True</c> when <paramref name="fileName" /> has an extension that the OS treats as
	/// directly runnable. Files without an extension return <c>False</c> — that case
	/// is handled separately by the caller.
	/// </summary>
	public static bool IsExecutable(string fileName)
	{
		string extension = Path.GetExtension(fileName);

		if (string.IsNullOrEmpty(extension))
		{
			return false;
		}

		return _executableExtensions.Contains(extension);
	}
	#endregion
}
