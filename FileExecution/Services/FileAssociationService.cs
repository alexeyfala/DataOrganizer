/*
 * Path to the "Open with" list in the registry:
 * HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.txt\OpenWithList
 */

using FileExecution.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FileExecution.Services;

/// <inheritdoc cref="IFileAssociationService" />
[SupportedOSPlatform("windows")]
[ExcludeFromCodeCoverage(Justification = "Asks the Windows shell for file associations")]
public sealed partial class FileAssociationService : IFileAssociationService
{
	#region Methods
	/// <inheritdoc />
	public bool HasAssociatedApp(string fileExtension)
	{
		// A packaged app names no executable: the shell starts it through its DelegateExecute handler.
		return QueryAssociation(AssocStr.Executable, fileExtension) is { } executablePath
			? !executablePath.EndsWith("OpenWith.exe", StringComparison.OrdinalIgnoreCase)
			: QueryAssociation(AssocStr.DelegateExecute, fileExtension) is not null;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads <paramref name="field" /> of the association of <paramref name="fileExtension" />; <c>null</c> when the type
	/// has none, the "Unknown" type of Windows not counted.
	/// </summary>
	private static string? QueryAssociation(AssocStr field, string fileExtension)
	{
		uint length = 0;

		uint result = AssocQueryString(
			AssocF.InitIgnoreUnknown,
			field,
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
			AssocF.InitIgnoreUnknown,
			field,
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
	#endregion

	#region Nested Types
	/// <summary>
	/// Options of an association query, <c>ASSOCF</c> of the Windows SDK.
	/// </summary>
	[Flags]
	private enum AssocF
	{
		/// <summary>
		/// The "Unknown" type of a file without an association fails the query instead of answering with it.
		/// </summary>
		InitIgnoreUnknown = 0x400
	}

	/// <summary>
	/// Field of an association to read, <c>ASSOCSTR</c> of the Windows SDK.
	/// </summary>
	private enum AssocStr
	{
		/// <summary>
		/// Executable of the open command.
		/// </summary>
		Executable = 2,

		/// <summary>
		/// Class of the COM object that runs the open command in place of an executable.
		/// </summary>
		DelegateExecute = 18
	}
	#endregion
}
