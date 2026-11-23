using Avalonia.Automation.Peers;

namespace DataOrganizer.UI.Controls;

/// <summary>
/// Automation peer of a <see cref="FlyoutButton" />, named by its header.
/// </summary>
internal sealed class FlyoutButtonAutomationPeer : ButtonAutomationPeer
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="FlyoutButtonAutomationPeer" /> class.
	/// </summary>
	public FlyoutButtonAutomationPeer(FlyoutButton owner)
		: base(owner)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override string? GetNameCore()
	{
		// The content is built from the header, and a button without one is named as any other.
		return ((FlyoutButton)Owner).Header ?? base.GetNameCore();
	}
	#endregion
}
