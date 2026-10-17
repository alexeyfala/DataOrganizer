using DataOrganizer.Dto.Dialogs;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Dialogs;

/// <summary>
/// Provides dialogs for selecting file system entries over the main window.
/// </summary>
public interface IFileSystemPicker
{
	#region Methods
	/// <summary>
	/// Opens a dialog for saving the file and returns the path to it.
	/// </summary>
	Task<string?> SaveFileAsync(FileSaveParameters parameters);

	/// <summary>
	/// Opens a dialog for selecting files and returns paths to them.
	/// </summary>
	Task<string[]> SelectFilesAsync(FileOpenParameters parameters);
	#endregion
}
