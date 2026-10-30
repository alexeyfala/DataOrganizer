using Hierarchy.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchyNotes.Interfaces;

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
		ExplorerItemDtoBase item,
		string? note,
		CancellationToken token = default);
	#endregion
}
