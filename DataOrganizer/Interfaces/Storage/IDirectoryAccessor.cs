namespace DataOrganizer.Interfaces.Storage;

/// <summary>
/// Provides tools to work with directories.
/// </summary>
public interface IDirectoryAccessor
{
	#region Methods
	/// <summary>
	/// Opens the directory containing the application and selects it.
	/// </summary>
	void OpenAppDirectory();

	/// <summary>
	/// Opens the specified directory.
	/// </summary>
	void OpenDirectory(string directoryPath);

	/// <summary>
	/// Opens the directory containing the specified file (or bundle) and selects it.
	/// </summary>
	void RevealFile(string filePath);
	#endregion
}
