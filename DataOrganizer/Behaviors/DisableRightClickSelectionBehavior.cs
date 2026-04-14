using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

internal sealed class DisableRightClickSelectionBehavior : Behavior<Interactive>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		if (AssociatedObject is null || !e.Properties.IsRightButtonPressed)
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
