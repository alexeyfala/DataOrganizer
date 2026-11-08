using FileSystemOperations.Interfaces;
using Shared.Common;
using Shared.Interfaces;

namespace FileSystemOperations.Services;

/// <summary>
/// Reveals files through the "open" command. Bringing an already opened Finder window
/// to the front is not supported.
/// </summary>
public sealed class MacOSExplorerManager : IExplorerManager
{
	#region Data
	/// <inheritdoc cref="IProcessLauncher" />
	private readonly IProcessLauncher _processLauncher;
	#endregion

	#region Constructors
	public MacOSExplorerManager(IProcessLauncher processLauncher) => _processLauncher = processLauncher;
	#endregion

	#region Methods
	/// <inheritdoc />
	public bool TryForegroundFolder(string folderPath, string? selectItemPath = null) => false;

	/// <inheritdoc />
	public bool TryRevealFile(string filePath)
	{
		try
		{
			_processLauncher.StartProcess(PlatformInfo.FileOpener, ["-R", filePath]);

			return true;
		}
		catch
		{
			// The opener is missing or refused to start — the caller falls back to the folder.
			return false;
		}
	}
	#endregion
}
