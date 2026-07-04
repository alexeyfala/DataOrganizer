using Shared.Enums;

namespace DataOrganizer.Interfaces.Explorer;

/// <summary>
/// Provides tools for working with file managers in <see cref="OperatingSystemType.Linux" />.
/// </summary>
public interface ILinuxExplorerManager
{
	#region Methods
	/// <summary>
	/// Tries to bring an already opened folder to the front via the freedesktop D-Bus interface
	/// <c>org.freedesktop.FileManager1.ShowFolders</c>.
	/// </summary>
	bool TryForegroundFolder(string folderPath);

	/// <summary>
	/// Tries to open a file manager with the specified file selected via the freedesktop D-Bus
	/// interface <c>org.freedesktop.FileManager1.ShowItems</c>.
	/// </summary>
	bool TryRevealFile(string filePath);
	#endregion
}
