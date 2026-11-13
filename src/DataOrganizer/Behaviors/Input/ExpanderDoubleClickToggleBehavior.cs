using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactivity;
using System.Linq;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Lets the header of an <see cref="Expander" /> that it is attached to expand and collapse it only on a double click,
/// while the buttons inside the header keep their single clicks.
/// </summary>
internal sealed class ExpanderDoubleClickToggleBehavior : Behavior<Interactive>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		// A button inside the header takes its own clicks, while the expander's button lies outside the header.
		if (e.ClickCount == 2
			|| e.Source is not Visual visual
			|| visual.GetSelfAndLogicalAncestors().TakeWhile(x => x != AssociatedObject).OfType<Button>().Any())
		{
			return;
		}

		e.Handled = true;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.AddHandler(
			InputElement.PointerPressedEvent,
			AssociatedObject_PointerPressed,
			RoutingStrategies.Tunnel);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.RemoveHandler(
			InputElement.PointerPressedEvent,
			AssociatedObject_PointerPressed);
	}
	#endregion
}
