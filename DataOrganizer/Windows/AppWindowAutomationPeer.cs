using Avalonia.Automation;
using Avalonia.Automation.Peers;

namespace DataOrganizer.Windows;

/// <summary>
/// Automation peer of a window of the application, named by its automation name, or by its title without one.
/// </summary>
internal sealed class AppWindowAutomationPeer : WindowAutomationPeer
{
	#region Constructors
	public AppWindowAutomationPeer(AppWindowBase owner) : base(owner)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override string? GetNameCore()
	{
		// The peer of Avalonia reads the title alone, while a window without a caption may keep its default title.
		return AutomationProperties.GetName(Owner) is { Length: > 0 } name
			? name
			: base.GetNameCore();
	}
	#endregion
}
