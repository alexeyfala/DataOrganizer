using FileExecution.Interfaces;
using Shared.Interfaces;

namespace FileExecution.Services;

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
