using DataOrganizer.Documents.Notepad.Interfaces;

namespace DataOrganizer.Documents.Notepad.Services;

/// <inheritdoc cref="INotepadSessionState" />
public sealed class NotepadSessionState : INotepadSessionState
{
	#region Properties
	/// <inheritdoc />
	public int? PreviousTabNumber { get; set; }
	#endregion
}
