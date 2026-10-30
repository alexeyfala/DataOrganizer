using DatasetRecords.Models;
using System.Collections.ObjectModel;

namespace DatasetRecords.Dto;

/// <summary>
/// A dragged record and the place it is dropped at.
/// </summary>
public sealed record DatasetRecordMove
{
	#region Properties
	/// <summary>
	/// Insertion slot in <see cref="Target" />, seen before the move.
	/// </summary>
	public required int Index { get; init; }

	/// <summary>
	/// The dragged record.
	/// </summary>
	public required DatasetRecordBase Record { get; init; }

	/// <summary>
	/// Collection the record is dropped into.
	/// </summary>
	public required ObservableCollection<DatasetRecordBase> Target { get; init; }
	#endregion
}
