using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Shared.Common;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides dialogs for selecting file system entries.
/// </summary>
public interface IFileSystemPicker
{
	#region Properties
	/// <summary>
	/// File types for import/export application objects.
	/// </summary>
	static FilePickerFileType[] ImportExportFilePickerTypes { get; } =
	[
		new("All Supported Files")
		{
			Patterns = [$"*{JsonExt}", $"*{XmlExt}", $"*{AppUtils.SQLiteExtension}"],
			MimeTypes = [JsonMime, XmlMime, SqliteMime]
		},
		new("JSON File")
		{
			Patterns = [$"*{JsonExt}"],
			MimeTypes = [JsonMime]
		},
		new("XML File")
		{
			Patterns = [$"*{XmlExt}"],
			MimeTypes = [XmlMime]
		},
		new("SQLite Database File")
		{
			Patterns = [$"*{AppUtils.SQLiteExtension}"],
			MimeTypes = [SqliteMime]
		}
	];
	#endregion

	#region Data
	/// <summary>
	/// JSON file extension.
	/// </summary>
	const string JsonExt = ".json";

	/// <summary>
	/// MIME type for JSON files.
	/// </summary>
	const string JsonMime = "application/json";

	/// <summary>
	/// MIME type for SQLite database files.
	/// </summary>
	const string SqliteMime = "application/x-sqlite3";

	/// <summary>
	/// XML file extension.
	/// </summary>
	const string XmlExt = ".xml";

	/// <summary>
	/// MIME type for XML files.
	/// </summary>
	const string XmlMime = "application/xml";
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
