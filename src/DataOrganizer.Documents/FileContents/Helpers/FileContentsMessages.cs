using DataOrganizer.Shared.Properties;

namespace DataOrganizer.Documents.FileContents.Helpers;

/// <summary>
/// Composes the messages about the contents of a named file.
/// </summary>
public static class FileContentsMessages
{
	#region Methods
	/// <summary>
	/// Message that the named file holds no contents.
	/// </summary>
	public static string Empty(string fileName) => $@"{Strings.ThereIsNoContentFor} ""{fileName}""";

	/// <summary>
	/// Message that the contents of the named file failed to load.
	/// </summary>
	public static string FailedToLoad(string? fileName) => $@"{Strings.FailedToLoadFileContents} ""{fileName}""";

	/// <summary>
	/// Message that the contents of the named file are not text.
	/// </summary>
	public static string NotText(string fileName) => $@"{Strings.NonTextFileContents}: ""{fileName}""";
	#endregion
}
