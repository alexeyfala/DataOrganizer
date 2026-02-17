using DataOrganizer.DTO;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Monitors changes in a file.
/// </summary>
public interface IFileChangeTracker
{
	#region Methods
	/// <summary>
	/// Tracks changes of the executed file.
	/// </summary>
	Task TrackChangesAsync(TrackChangesParameters parameters, CancellationToken token = default);
	#endregion
}
