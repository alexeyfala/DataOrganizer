using DataOrganizer.DTO.Entities.Models;
using System;
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
	Task TrackChangesAsync(
		FileModelDto dto,
		string filePath,
		byte[] contents,
		SemaphoreSlim semaphore,
		Predicate<Guid> condition,
		CancellationToken token = default);	
	#endregion
}
