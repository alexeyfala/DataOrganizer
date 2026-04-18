using DataOrganizer.Abstract;
using System.Threading;

namespace DataOrganizer.DTO;

public sealed class TrackChangesParameters : ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// A path to the file.
	/// </summary>
	public required string FilePath { get; init; }

	/// <inheritdoc cref="SemaphoreSlim" />
	public required SemaphoreSlim Semaphore { get; init; }
	#endregion
}
