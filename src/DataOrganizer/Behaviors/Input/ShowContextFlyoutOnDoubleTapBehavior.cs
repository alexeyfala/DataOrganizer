using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Opens the <see cref="Control.ContextFlyout" /> of the associated control on a double click.
/// </summary>
internal sealed class ShowContextFlyoutOnDoubleTapBehavior : Behavior<Control>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.DoubleTappedEvent" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_DoubleTapped(
		object? sender,
		TappedEventArgs e)
	{
		if (sender is not Control { ContextFlyout: { } flyout } control)
		{
			return;
		}

		flyout.ShowAt(control);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.AddHandler(
			InputElement.DoubleTappedEvent,
			AssociatedObject_DoubleTapped);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.RemoveHandler(
			InputElement.DoubleTappedEvent,
			AssociatedObject_DoubleTapped);
	}
	#endregion
}
