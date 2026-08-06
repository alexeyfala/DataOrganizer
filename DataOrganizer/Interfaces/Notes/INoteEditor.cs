using DataOrganizer.DTO.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Notes;

/// <summary>
/// Stores the note of an explorer object in the database and in the hierarchy.
/// </summary>
public interface INoteEditor
{
	#region Methods
	/// <summary>
	/// Overwrites the note of <paramref name="item" />; blank <paramref name="note" /> removes it.
	/// </summary>
	Task<bool> EditAsync(
		ExplorerModelBaseDto item,
		string? note,
		DateTime updatedDate,
		CancellationToken token = default);
	#endregion
}
