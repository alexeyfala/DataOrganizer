namespace DataOrganizer.Interfaces.Explorer;

/// <summary>
/// Provides tools for working with the file manager of the operating system in use.
/// </summary>
public interface IExplorerManager
{
	#region Methods
	/// <summary>
	/// Tries to bring an already opened folder to the front and, where the file manager
	/// supports it, select an item inside that folder.
	/// </summary>
	bool TryForegroundFolder(string folderPath, string? selectItemPath = null);

	/// <summary>
	/// Tries to open a file manager window with the specified file selected.
	/// </summary>
	bool TryRevealFile(string filePath);
	#endregion
}
