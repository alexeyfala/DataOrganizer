using DataOrganizer.SystemIntegration.FileExecution.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.FileExecution.Interfaces;

/// <summary>
/// Monitors changes in a file.
/// </summary>
public interface IFileChangeTracker
{
	#region Methods
	/// <summary>
	/// Tracks changes of the executing file.
	/// </summary>
	Task TrackChangesAsync(TrackChangesParameters parameters, CancellationToken token = default);
	#endregion
}
