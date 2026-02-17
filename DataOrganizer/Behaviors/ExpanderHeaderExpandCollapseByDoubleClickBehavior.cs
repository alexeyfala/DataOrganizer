using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Extensions;

namespace DataOrganizer.Behaviors;

internal sealed class ExpanderHeaderExpandCollapseByDoubleClickBehavior : Behavior<Interactive>
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
			|| visual.HasLogicalParent<Button>(x => !string.Equals(x.Name, "PART_toggle")))
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
