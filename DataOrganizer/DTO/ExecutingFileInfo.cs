using System.Threading;

namespace DataOrganizer.DTO;

/// <summary>
/// Information about an executing file.
/// </summary>
internal sealed class ExecutingFileInfo
{
	#region Properties
	/// <inheritdoc cref="CancellationTokenSource" />
	public CancellationTokenSource Cancellation { get; }

	/// <summary>
	/// Directory path.
	/// </summary>
	public string DirectoryPath { get; }

	/// <summary>
	/// File path.
	/// </summary>
	public string FilePath { get; }

	/// <summary>
	/// Process Id.
	/// </summary>
	public int ProcessId { get; }
	#endregion

	#region Constructors
	public ExecutingFileInfo(
		CancellationTokenSource cancellation,
		string filePath,
		string directoryPath,
		in int processId)
	{
		Cancellation = cancellation;

		FilePath = filePath;

		DirectoryPath = directoryPath;

		ProcessId = processId;
	}
	#endregion
}
