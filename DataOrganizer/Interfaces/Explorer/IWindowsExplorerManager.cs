using Shared.Enums;

namespace DataOrganizer.Interfaces.Explorer;

/// <summary>
/// Provides tools for working with "Explorer" in <see cref="OperatingSystemType.Windows" />.
/// </summary>
public interface IWindowsExplorerManager
{
	#region Methods
	/// <summary>
	/// Tryes to bring folder to the front.
	/// </summary>
	bool TryForegroundFolder(string folderPath);
	#endregion
}
