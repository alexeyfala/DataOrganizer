using Shared.Enums;

namespace DataOrganizer.Interfaces.Execution;

/// <summary>
/// Contains methods that allow you to determine which applications to use to open files accepted by the <see cref="OperatingSystemKind.Windows" /> operating system.
/// </summary>
public interface IFileAssociationService
{
	#region Methods
	/// <summary>
	/// Tries to find a path to the executable application file by file extension.
	/// </summary>
	string? FindApplicationByExtension(string fileExtension);
	#endregion
}
