using NotepadDocuments.Interfaces;

namespace NotepadDocuments.Services;

/// <inheritdoc cref="INotepadSessionState" />
public sealed class NotepadSessionState : INotepadSessionState
{
	#region Properties
	/// <inheritdoc />
	public int? PreviousTabNumber { get; set; }
	#endregion
}
