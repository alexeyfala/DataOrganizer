namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// Schedules an automatic clear of the system clipboard shortly after this application places
/// sensitive content there, provided the content is still present when the timeout elapses.
/// </summary>
public interface IClipboardAutoClear
{
	#region Methods
	/// <summary>
	/// Starts (or restarts) the auto-clear countdown.
	/// </summary>
	void Arm();
	#endregion
}
