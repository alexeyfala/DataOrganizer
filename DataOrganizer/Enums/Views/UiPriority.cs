namespace DataOrganizer.Enums.Views;

/// <summary>
/// Priority of work posted to the UI thread.
/// </summary>
public enum UiPriority
{
	/// <summary>
	/// The usual priority of the dispatcher.
	/// </summary>
	Default,

	/// <summary>
	/// After the other non-idle work has completed.
	/// </summary>
	Background
}
