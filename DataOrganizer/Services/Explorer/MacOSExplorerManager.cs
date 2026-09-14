using DataOrganizer.Interfaces.Explorer;
using Shared.Common;
using System.Diagnostics;

namespace DataOrganizer.Services.Explorer;

/// <summary>
/// Reveals files through the "open" command. Bringing an already opened Finder window
/// to the front is not supported.
/// </summary>
public sealed class MacOSExplorerManager : IExplorerManager
{
	#region Methods
	/// <inheritdoc />
	public bool TryForegroundFolder(string folderPath, string? selectItemPath = null) => false;

	/// <inheritdoc />
	public bool TryRevealFile(string filePath)
	{
		try
		{
			_ = Process.Start(PlatformInfo.FileOpener, GetReveal(filePath));

			return true;
		}
		catch
		{
			// The opener is missing or refused to start — the caller falls back to the folder.
			return false;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Combines the path with the folder expansion argument.
	/// </summary>
	private static string GetReveal(string argument) => $@"-R ""{argument}""";
	#endregion
}
