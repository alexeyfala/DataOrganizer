using DataOrganizer.Shared.Interfaces;
using DataOrganizer.SystemIntegration.FileExecution.Interfaces;

namespace DataOrganizer.SystemIntegration.FileExecution.Services;

/// <summary>
/// Stand-in for operating systems that cannot tell which programs hold a file; finds none.
/// </summary>
public sealed class UnsupportedExecutingFileHolderFinder : IExecutingFileHolderFinder
{
	#region Methods
	/// <inheritdoc />
	public IExecutingFileProcess[] FindHolders(string filePath) => [];
	#endregion
}
