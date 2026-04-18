using System.Threading;

namespace DataOrganizer.DTO;

/// <summary>
/// The pair of <see cref="FilePath" /> and <see cref="DirectoryPath" /> values.
/// </summary>
internal sealed class ExecutedFileInfo
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
	public ExecutedFileInfo(
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
