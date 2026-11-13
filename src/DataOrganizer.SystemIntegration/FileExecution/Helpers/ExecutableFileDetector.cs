using DataOrganizer.Infrastructure.FileSystemOperations.Helpers;
using System;
using System.Collections.Frozen;

namespace DataOrganizer.SystemIntegration.FileExecution.Helpers;

/// <summary>
/// Detects whether a file is potentially executable based on its extension.
/// </summary>
public static class ExecutableFileDetector
{
	#region Data
	// Stryker disable all : a list of extensions; a test per entry would copy it
	/// <summary>
	/// Case-insensitive set of file extensions whose double-click / shell-execute action runs code on Windows, Linux or
	/// macOS: direct executables, shell / interpreted scripts, installers, shortcuts and every type Outlook blocks as a
	/// level 1 attachment.
	/// </summary>
	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> ExecutableExtensions = new[]
	{
	#region Windows
		".exe",					// Native executables
		".com",					// DOS-era executables
		".scr",					// Screensavers (actually executables)
		".pif",					// Program info files (executable on legacy systems)
		".cpl",					// Control Panel items
		".msi",					// Windows Installer packages
		".msp",					// Windows Installer patches
		".msu",					// Windows Update Standalone Installer
		".bat",					// Batch scripts
		".cmd",					// Command scripts
		".vbs",					// VBScript
		".vbe",					// Encoded VBScript
		".js",					// JScript (Windows Script Host, not browser-only)
		".jse",					// Encoded JScript
		".ws",					// Windows Script
		".wsf",					// Windows Script File
		".wsh",					// Windows Script Host settings
		".ps1",					// PowerShell script
		".psm1",				// PowerShell module
		".psd1",				// PowerShell data file
		".ps1xml",				// PowerShell XML config
		".hta",					// HTML Application (runs with full local privileges)
		".chm",					// Compiled HTML Help (can run code)
		".msc",					// Microsoft Management Console snap-in
		".gadget",				// Windows Desktop Gadget
		".jar",					// Java archive (runs if JRE installed)
		".jnlp",				// Java Web Start descriptor (downloads and runs an application)
		".reg",					// Registry file (modifies registry on double-click)
		".lnk",					// Shortcut (targets executable)
		".url",					// Internet shortcut
		".application",			// ClickOnce deployment manifest
		".appref-ms",			// ClickOnce application reference
		".appinstaller",		// App Installer descriptor
		".appx",				// Application package
		".appxbundle",			// Application package bundle
		".msix",				// Application package
		".msixbundle",			// Application package bundle
		".inf",					// Setup information file (Install verb)
		".ins",					// Internet communication settings
		".isp",					// Internet communication service profile
		".scf",					// Shell command file
		".sct",					// Windows script component
		".wsc",					// Windows script component
		".xll",					// Excel add-in (a native library)
		".msh",					// Monad shell script
		".msh1",				// Monad shell script
		".msh2",				// Monad shell script
		".mshxml",				// Monad shell XML config
		".cdxml",				// PowerShell cmdlet definition
		".shb",					// Shell scrap shortcut
		".shs",					// Shell scrap object
		".settingcontent-ms",	// Settings shortcut (runs the command from DeepLink)
		".diagcab",				// Troubleshooting package
		// The rest of the level 1 types that Outlook blocks in attachments
		".ade",					// Access project extension
		".adp",					// Access project
		".apk",					// Android application package
		".appcontent-ms",		// Windows application content
		".asp",					// Active Server Page
		".aspx",				// Active Server Page Extended
		".asx",					// ASF redirector
		".bas",					// BASIC source code
		".bgi",					// Borland Graphics Interface
		".cab",					// Windows cabinet archive
		".cer",					// Security certificate
		".cnt",					// Help Workshop contents
		".crt",					// Certificate
		".der",					// DER-encoded X.509 certificate
		".fxp",					// FoxPro compiled program
		".grp",					// Program group
		".hlp",					// Windows Help file
		".hpj",					// Help project
		".htc",					// HTML component
		".img",					// Disk image (mounts on open)
		".iso",					// Optical disc image (mounts on open)
		".its",					// Internet document set
		".library-ms",			// Windows library description
		".mad",					// Access module shortcut
		".maf",					// Access shortcut
		".mag",					// Access diagram shortcut
		".mam",					// Access macro shortcut
		".maq",					// Access query shortcut
		".mar",					// Access report shortcut
		".mas",					// Access stored procedures
		".mat",					// Access table shortcut
		".mau",					// Media attachment unit
		".mav",					// Access view shortcut
		".maw",					// Access data access page
		".mcf",					// Media container format
		".mda",					// Access add-in
		".mdb",					// Access application or database
		".mde",					// Access compiled database
		".mdt",					// Access add-in data
		".mdw",					// Access workgroup information
		".mdz",					// Access wizard template
		".mht",					// Web archive
		".mhtml",				// Web archive
		".msh1xml",				// Monad shell XML config
		".msh2xml",				// Monad shell XML config
		".mst",					// Windows Installer transform
		".ops",					// Office profile settings
		".osd",					// Open Software Description
		".pcd",					// Visual Test script
		".plg",					// Developer Studio build log
		".prf",					// System settings file
		".prg",					// Program file
		".printerexport",		// Printer backup
		".ps2",					// PowerShell script
		".ps2xml",				// PowerShell XML config
		".psc1",				// PowerShell console file
		".psc2",				// PowerShell console file
		".psdm1",				// PowerShell file
		".pssc",				// PowerShell session configuration
		".pst",					// Outlook data file
		".search-ms",			// Saved search
		".theme",				// Desktop theme
		".tmp",					// Temporary file
		".udl",					// Universal data link
		".vb",					// Visual Basic source
		".vbp",					// Visual Basic project
		".vhd",					// Virtual hard disk (mounts on open)
		".vhdx",				// Virtual hard disk (mounts on open)
		".vsmacros",			// Visual Studio macro project
		".vsw",					// Visio workspace
		".webpnp",				// Internet printing file
		".website",				// Pinned site shortcut
		".wsb",					// Windows Sandbox configuration (runs its logon command)
		".xbap",				// XAML browser application
		".xnk",					// Exchange public folder shortcut
	#endregion

	#region Linux / Unix
		".sh",					// Shell scripts (POSIX sh)
		".bash",				// Bash scripts
		".zsh",					// Zsh scripts
		".ksh",					// Korn shell scripts
		".csh",					// C shell scripts
		".fish",				// Fish shell scripts
		".run",					// Generic self-extracting Linux installers
		".appimage",			// AppImage portable executables
		".flatpakref",			// Flatpak reference (triggers install)
		".flatpak",				// Flatpak bundles
		".snap",				// Snap packages
		".deb",					// Debian packages (installer prompts on open)
		".rpm",					// RPM packages
		".desktop",				// Desktop entries (Exec= field runs arbitrary command)
	#endregion

	#region macOS
		".app",					// macOS application bundles
		".command",				// Executable shell scripts launched by Terminal
		".tool",				// Terminal tool scripts
		".dmg",					// Disk images (often contain installers)
		".pkg",					// macOS installer packages
		".mpkg",				// macOS meta installer packages
		".scpt",				// Compiled AppleScript
		".scptd",				// AppleScript bundles
		".applescript",			// AppleScript source
		".action",				// Automator actions
		".workflow",			// Automator workflows
		".osax",				// Scripting Additions
	#endregion

	#region Cross-platform interpreted scripts
		".py",					// Python
		".pyw",					// Python (no-console on Windows)
		".pyc",					// Python compiled
		".pyo",					// Python optimized compiled
		".pyz",					// Python zip application
		".pyzw",				// Python zip application (no-console on Windows)
		".pl",					// Perl
		".rb",					// Ruby
		".lua",					// Lua
		".php",					// PHP CLI scripts
		".tcl"					// Tcl scripts
	#endregion
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();
	// Stryker restore all
	#endregion

	#region Methods
	/// <summary>
	/// <c>True</c> when <paramref name="fileName" /> has an extension that the OS treats as
	/// directly runnable. Files without an extension return <c>False</c> — that case
	/// is handled separately by the caller.
	/// </summary>
	public static bool IsExecutable(string? fileName)
	{
		ReadOnlySpan<char> extension = FileNameHelper.GetExtension(fileName);

		if (extension.IsEmpty)
		{
			return false;
		}

		return ExecutableExtensions.Contains(extension);
	}
	#endregion
}
