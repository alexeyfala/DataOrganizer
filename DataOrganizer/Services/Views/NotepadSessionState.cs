using DataOrganizer.Interfaces.Views;

namespace DataOrganizer.Services.Views;

/// <inheritdoc cref="INotepadSessionState" />
internal sealed class NotepadSessionState : INotepadSessionState
{
	#region Properties
	/// <inheritdoc />
	public int? PreviousTabNumber { get; set; }
	#endregion
}
