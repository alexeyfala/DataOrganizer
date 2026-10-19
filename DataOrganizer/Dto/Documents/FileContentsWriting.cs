using DataOrganizer.Enums.Documents;
using System;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Outcome of writing the contents of a file.
/// </summary>
public sealed record FileContentsWriting
{
	#region Properties
	/// <inheritdoc cref="FileContentsWriteStatus" />
	public required FileContentsWriteStatus Status { get; init; }

	/// <summary>
	/// Date of change written with the contents; <c>null</c> unless an edit was written.
	/// </summary>
	public DateTime? UpdatedAt { get; init; }
	#endregion
}
