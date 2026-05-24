using Shared.Enums;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Contains methods that allow you to determine which applications to use to open files accepted by the <see cref="OperatingSystemType.Windows" /> operating system.
/// </summary>
public interface IFileAssociationService
{
	#region Methods
	/// <summary>
	/// Returns the path to the executable application file by file extension.
	/// </summary>
	string? GetApplicationByExtension(string fileExtension);

	/// <summary>
	/// Returns the path to the executable application file by the absolute path of existing file.
	/// For the method to work, the file must exist at the specified path.
	/// </summary>
	string? GetApplicationByPath(string absoluteFilePath);
	#endregion
}
