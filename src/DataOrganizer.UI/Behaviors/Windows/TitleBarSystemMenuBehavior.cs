using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.UI.Behaviors.Windows;

/// <summary>
/// Lets Windows open the system menu of the window on a right click of the attached title bar.
/// </summary>
internal sealed class TitleBarSystemMenuBehavior : Behavior<Interactive>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		if (!OperatingSystem.IsWindows() || !e
			.Properties
			.IsRightButtonPressed)
		{
			return;
		}

		// Avalonia opens the menu when the button goes up over the caption; the capture it takes on a press would turn
		// that release into a client one.
		e
			.Pointer
			.Capture(null);
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
