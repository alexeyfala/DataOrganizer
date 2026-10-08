namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Holds the state of the notepad that lives only for the current application session and is never written to the
/// settings files.
/// </summary>
public interface INotepadSessionState
{
	#region Properties
	/// <summary>
	/// Number of the tab Ctrl+Tab goes back to.
	/// </summary>
	int? PreviousTabNumber { get; set; }
	#endregion
}
