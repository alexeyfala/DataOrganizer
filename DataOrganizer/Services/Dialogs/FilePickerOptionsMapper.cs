using Avalonia.Platform.Storage;
using DataOrganizer.Dto.Dialogs;
using System.Linq;

namespace DataOrganizer.Services.Dialogs;

/// <summary>
/// Turns the parameters of the file dialogs into the options of the storage provider.
/// </summary>
internal static class FilePickerOptionsMapper
{
	#region Methods
	/// <summary>
	/// Returns the options of a dialog that chooses files to open.
	/// </summary>
	public static FilePickerOpenOptions ToOpenOptions(FileOpenParameters parameters)
	{
		return new()
		{
			AllowMultiple = parameters.AllowMultiple,
			FileTypeFilter = [.. parameters.FileTypes.Select(ToFileType)],
			Title = parameters.Title
		};
	}

	/// <summary>
	/// Returns the options of a dialog that chooses a file to save.
	/// </summary>
	public static FilePickerSaveOptions ToSaveOptions(FileSaveParameters parameters)
	{
		return new()
		{
			DefaultExtension = parameters.DefaultExtension,
			FileTypeChoices = [.. parameters.FileTypes.Select(ToFileType)],
			ShowOverwritePrompt = parameters.ShowOverwritePrompt,
			SuggestedFileName = parameters.SuggestedFileName,
			Title = parameters.Title
		};
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the file type of the storage provider for a type of files.
	/// </summary>
	private static FilePickerFileType ToFileType(FileTypeFilter filter)
	{
		return new(filter.Name)
		{
			MimeTypes = filter.MimeTypes,
			Patterns = filter.Patterns
		};
	}
	#endregion
}
