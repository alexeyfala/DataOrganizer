using DataOrganizer.Interfaces;
using System;
using System.Runtime.InteropServices;

/*
 * Path to the "Open with" list in the registry:
 * HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.txt\OpenWithList
 */

namespace DataOrganizer.Services;

/// <inheritdoc cref="IFileAssociationService" />
public sealed partial class FileAssociationService : IFileAssociationService
{
	#region Methods
	/// <inheritdoc />
	public string? GetApplicationByExtension(string fileExtension)
	{
		uint length = 0;

		uint result = AssocQueryString(
			AssocF.None,
			AssocStr.Executable,
			fileExtension,
			null,
			null,
			ref length);

		if (result != 1)
		{
			return null;
		}

		char[] buffer = new char[length];

		result = AssocQueryString(
			AssocF.None,
			AssocStr.Executable,
			fileExtension,
			null,
			buffer,
			ref length);

		if (result != 0)
		{
			return null;
		}

		int nullTerminator = Array.IndexOf(buffer, '\0');

		return new string(buffer, 0, nullTerminator >= 0 ? nullTerminator : buffer.Length);
	}

	/// <inheritdoc />
	public string? GetApplicationByPath(string absoluteFilePath)
	{
		char[] buffer = new char[1024];

		FindExecutable(absoluteFilePath, string.Empty, buffer);

		int nullTerminator = Array.IndexOf(buffer, '\0');

		if (nullTerminator <= 0)
		{
			return null;
		}

		return new string(buffer, 0, nullTerminator);
	}
	#endregion

	#region Native
	[LibraryImport("Shlwapi.dll", EntryPoint = "AssocQueryStringW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial uint AssocQueryString(
		AssocF flags,
		AssocStr str,
		string pszAssoc,
		string? pszExtra,
		[Out] char[]? pszOut,
		ref uint pcchOut);

	[LibraryImport("shell32.dll", EntryPoint = "FindExecutableW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial long FindExecutable(
		string lpFile,
		string lpDirectory,
		[Out] char[] lpResult);
	#endregion

	#region Nested Types
	[Flags]
	private enum AssocF
	{
		None = 0,
		Init_NoRemapIniFile = 0x1,
		Init_ByExeName = 0x2,
		Open_ByExeName = 0x4,
		Init_DefaultToStar = 0x8,
		Init_FixedProgId = 0x10,
		Init_ForcedDefault = 0x20,
		Init_NoUserSettings = 0x40,
		Init_NotCachable = 0x80,
		Xc_Noconsistent = 0x100,
		Init_NoUserOverrides = 0x200,
		Init_AnySchema = 0x400,
		Init_SkipIfNoFile = 0x800,
		Open_NewProcess = 0x1000,
		Open_NoZoneChecks = 0x2000,
		Open_RequireAssoc = 0x4000,
		Init_NoExePath = 0x8000,
		Init_IgnoreBaseClass = 0x10000,
		Init_NoDefault = 0x20000,
		Init_UserSetting = 0x40000,
		Init_LocalMachineSetting = 0x80000,
		Init_QueryOnly = 0x100000,
		Init_NoUserChoice = 0x200000,
		Init_IncludeRecommended = 0x400000,
		Init_SkipIfNoHandler = 0x800000,
		Init_NoRegistry = 0x1000000,
		Init_NoDefaultIcon = 0x2000000,
		Init_NoOpenWith = 0x4000000,
		Init_NoInternetOpenWith = 0x8000000,
		Init_NoInternetDefault = 0x10000000,
		Init_NoInternetProtocols = 0x20000000,
		Init_NoInternetFileTypes = 0x40000000,
		//Init_NoInternetSearch = 0x80000000
	}

	private enum AssocStr
	{
		Command = 1,
		Executable,
		FriendlyDocName,
		FriendlyAppName,
		DDECommand,
		DDETopic,
		InfoTip,
		QuickTip,
		TileInfo,
		ContentType,
		DefaultIcon,
		ShellNewValue,
		DisplayName,
		EditFlags,
		OpenWithList,
		OpenWithProgids,
		FileExt,
		Max
	}
	#endregion
}
