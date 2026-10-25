using Hierarchy.Dto;

namespace HierarchyNotes.Interfaces;

/// <summary>
/// Provides the note of an object as plain text.
/// </summary>
public interface INoteReader
{
	#region Methods
	/// <summary>
	/// Returns the note as plain text, or <c>null</c> when it is unavailable.
	/// </summary>
	string? ReadNote(ExplorerItemDtoBase? item);
	#endregion
}
