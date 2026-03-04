using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Shared.Common;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides dialogs for selecting file system entries.
/// </summary>
public interface IFileSystemEnrtyPicker
{
	#region Properties
	/// <summary>
	/// File types for import/export application objects.
	/// </summary>
	static FilePickerFileType[] ImportExportFilePickerTypes { get; } =
	[
		new("JSON File")
		{
			Patterns = [$"*{JsonExt}"],
			MimeTypes = ["application/json"]
		},
		new("XML File")
		{
			Patterns = [$"*{XmlExt}"],
			MimeTypes = ["application/xml"]
		},
		new("SQLite Database File")
		{
			Patterns = [$"*{AppUtils.SQLiteExtension}"],
			MimeTypes = ["application/x-sqlite3"]
		}
	];
	#endregion

	#region Data
	/// <summary>
	/// JSON file extension.
	/// </summary>
	const string JsonExt = ".json";

	/// <summary>
	/// XML file extension.
	/// </summary>
	const string XmlExt = ".xml";
	#endregion

	#region Methods
	/// <summary>
	/// Opens a dialog for saving the file and returns the path to it.
	/// </summary>
	Task<string?> SaveFileAsync<T>(FilePickerSaveOptions options) where T : Window;

	/// <summary>
	/// Opens a dialog for selecting files and returns paths to them.
	/// </summary>
	Task<string[]> SelectFilesAsync<T>(FilePickerOpenOptions options) where T : Window;
	#endregion
}
