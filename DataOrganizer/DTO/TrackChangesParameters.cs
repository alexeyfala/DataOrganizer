using DataOrganizer.DTO.Entities.Models;
using System;
using System.Threading;

namespace DataOrganizer.DTO;

public struct TrackChangesParameters
{
	#region Properties
	/// <summary>
	/// A condition to track changes.
	/// </summary>
	public required Predicate<Guid> Condition { get; init; }

	/// <summary>
	/// A contents of the file.
	/// </summary>
	public required byte[] Contents { get; set; }

	/// <summary>
	/// Encrypted password.
	/// </summary>
	public byte[]? EncryptedPassword { get; init; }

	/// <inheritdoc cref="FileModelDto" />
	public required FileModelDto File { get; init; }

	/// <summary>
	/// A path to the file.
	/// </summary>
	public required string FilePath { get; init; }

	/// <inheritdoc cref="SemaphoreSlim" />
	public required SemaphoreSlim Semaphore { get; init; }
	#endregion
}
