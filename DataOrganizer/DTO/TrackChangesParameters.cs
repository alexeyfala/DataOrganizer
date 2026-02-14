using DataOrganizer.Abstract;
using System;
using System.Threading;

namespace DataOrganizer.DTO;

public sealed class TrackChangesParameters : ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// A condition to track changes.
	/// </summary>
	public required Predicate<Guid> Condition { get; init; }

	/// <summary>
	/// A path to the file.
	/// </summary>
	public required string FilePath { get; init; }

	/// <inheritdoc cref="SemaphoreSlim" />
	public required SemaphoreSlim Semaphore { get; init; }
	#endregion
}
