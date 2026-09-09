using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactivity;
using System.Linq;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Expands and collapses the attached <see cref="Expander" /> on a double click of its header.
/// </summary>
internal sealed class ExpanderDoubleClickToggleBehavior : Behavior<Interactive>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		if (e.ClickCount == 2
			|| e.Source is not Visual visual
			|| visual.GetLogicalAncestors().OfType<Button>().Any(x => !string.Equals(x.Name, "PART_ToggleButton")))
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
