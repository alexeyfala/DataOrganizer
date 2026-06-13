using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides dialogs for selecting file system entries.
/// </summary>
public interface IFileSystemPicker
{
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
