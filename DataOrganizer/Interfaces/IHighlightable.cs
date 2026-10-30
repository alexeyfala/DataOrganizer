namespace DataOrganizer.Interfaces;

/// <summary>
/// A control that can play a one-shot highlight animation on itself.
/// </summary>
internal interface IHighlightable
{
	#region Methods
	/// <summary>
	/// Plays a single highlight pulse.
	/// </summary>
	void PulseHighlight();
	#endregion
}
