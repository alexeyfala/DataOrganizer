using DataOrganizer.Shared.Interfaces;

namespace DataOrganizer.SystemIntegration.FileExecution.Interfaces;

/// <summary>
/// Finds the programs that hold a file opened in the operating system.
/// </summary>
public interface IExecutingFileHolderFinder
{
	#region Methods
	/// <summary>
	/// Programs that hold <paramref name="filePath" /> open and may be killed; empty when none is found.
	/// </summary>
	IExecutingFileProcess[] FindHolders(string filePath);
	#endregion
}
