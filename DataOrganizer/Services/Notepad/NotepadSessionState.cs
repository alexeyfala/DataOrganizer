using DataOrganizer.Interfaces.Notepad;

namespace DataOrganizer.Services.Notepad;

/// <inheritdoc cref="INotepadSessionState" />
internal sealed class NotepadSessionState : INotepadSessionState
{
	#region Properties
	/// <inheritdoc />
	public int? PreviousTabNumber { get; set; }
	#endregion
}
