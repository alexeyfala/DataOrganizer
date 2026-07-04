using Serilog;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides tools to work with directories.
/// </summary>
public interface IDirectoryAccessor
{
	#region Methods
	/// <summary>
	/// Opens the application directory.
	/// </summary>
	void OpenAppDirectory(ILogger? logger = null);

	/// <summary>
	/// Opens a directory.
	/// </summary>
	void OpenDirectory(string directoryPath, ILogger? logger = null);

	/// <summary>
	/// Opens the directory containing the specified file (or bundle) and selects it.
	/// </summary>
	void RevealFile(string filePath, ILogger? logger = null);
	#endregion
}
