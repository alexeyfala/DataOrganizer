using Shared.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace FileExecution.Dto;

/// <summary>
/// Information about an executing file.
/// </summary>
internal sealed class ExecutingFileInfo
{
	#region Properties
	/// <inheritdoc cref="CancellationTokenSource" />
	public required CancellationTokenSource Cancellation { get; init; }

	/// <summary>
	/// Directory path.
	/// </summary>
	public required string DirectoryPath { get; init; }

	/// <summary>
	/// File path.
	/// </summary>
	public required string FilePath { get; init; }

	/// <summary>
	/// Process started to open the file; <c>null</c> when no process of its own started.
	/// </summary>
	public required IExecutingFileProcess? Process { get; init; }

	/// <summary>
	/// Background change-tracking task; <see cref="Task.CompletedTask" /> for read-only files.
	/// Must be awaited after <see cref="Cancellation" />.Cancel() and before disposing it.
	/// </summary>
	public required Task TrackerTask { get; init; }
	#endregion
}
