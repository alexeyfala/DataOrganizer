using Shared.Enums;

namespace FileExecution.Interfaces;

/// <summary>
/// Tells whether the <see cref="OperatingSystemKind.Windows" /> operating system has a program to open a type of file.
/// </summary>
public interface IFileAssociationService
{
	#region Methods
	/// <summary>
	/// <c>True</c> when a program opens files with <paramref name="fileExtension" />, and not only the "Open with" dialog of
	/// Windows.
	/// </summary>
	bool HasAssociatedApp(string fileExtension);
	#endregion
}
