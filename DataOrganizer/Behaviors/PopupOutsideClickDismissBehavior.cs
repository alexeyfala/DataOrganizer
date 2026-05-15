using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using System;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Closes the attached <see cref="Popup" /> when the user presses the pointer outside of it.
/// Drop-in replacement for <see cref="Popup.IsLightDismissEnabled" />, which interferes with
/// pointer capture in hosted controls such as <c>AvaloniaEdit.TextEditor</c>.
/// </summary>
internal sealed class PopupOutsideClickDismissBehavior : Behavior<Popup>
{
	#region Data
	/// <summary>
	/// Handler attached to <see cref="_topLevel" /> while the popup is open. Kept in a field so the
	/// exact same delegate instance can later be passed to <see cref="Interactive.RemoveHandler" />.
	/// </summary>
	private EventHandler<PointerPressedEventArgs>? _pressedHandler;

	/// <summary>
	/// <see cref="TopLevel" /> that currently owns <see cref="_pressedHandler" />.
	/// Captured at <see cref="Popup.Opened" /> time so the same instance is used to remove the
	/// handler, even if <see cref="AssociatedObject" /> gets reparented before <see cref="Popup.Closed" />.
	/// </summary>
	private TopLevel? _topLevel;
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.Opened += AssociatedObject_Opened;

		AssociatedObject.Closed += AssociatedObject_Closed;
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		if (AssociatedObject is not null)
		{
			AssociatedObject.Opened -= AssociatedObject_Opened;

			AssociatedObject.Closed -= AssociatedObject_Closed;
		}

		DetachPointerHandler();
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Popup.Closed" /> event handler.
	/// </summary>
	private void AssociatedObject_Closed(object? sender, EventArgs e) => DetachPointerHandler();

	/// <summary>
	/// <see cref="Popup.Opened" /> event handler.
	/// </summary>
	private void AssociatedObject_Opened(object? sender, EventArgs e)
	{
		if (AssociatedObject is null || TopLevel.GetTopLevel(AssociatedObject) is not { } topLevel)
		{
			return;
		}

		DetachPointerHandler();

		_topLevel = topLevel;

		_pressedHandler = OnTopLevelPointerPressed;

		// Tunnel + handledEventsToo: catch the press before any descendant can mark it Handled.
		// Overlay-layer popups share the visual tree with TopLevel, so presses inside the popup
		// also reach this handler and must be filtered out by OnTopLevelPointerPressed.
		_topLevel.AddHandler(
			InputElement.PointerPressedEvent,
			_pressedHandler,
			RoutingStrategies.Tunnel,
			handledEventsToo: true);
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler on <see cref="TopLevel" />.
	/// Closes <see cref="AssociatedObject" /> unless the press originated inside the popup content.
	/// </summary>
	private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (AssociatedObject is null || IsInsidePopupContent(e.Source as Visual))
		{
			return;
		}

		AssociatedObject.IsOpen = false;
	}
	#endregion

	#region Service
	/// <summary>
	/// Removes the <see cref="TopLevel" /> pointer handler if currently attached.
	/// </summary>
	private void DetachPointerHandler()
	{
		if (_topLevel is null || _pressedHandler is null)
		{
			return;
		}

		_topLevel.RemoveHandler(InputElement.PointerPressedEvent, _pressedHandler);

		_topLevel = null;

		_pressedHandler = null;
	}

	/// <summary>
	/// Returns <c>True</c> if <paramref name="source" /> is the popup content of
	/// <see cref="AssociatedObject" /> or its visual descendant.
	/// </summary>
	private bool IsInsidePopupContent(Visual? source)
	{
		if (source is null || AssociatedObject?.Child is not Visual content)
		{
			return false;
		}

		Visual? current = source;

		while (current is not null)
		{
			if (ReferenceEquals(current, content))
			{
				return true;
			}

			current = current.GetVisualParent();
		}

		return false;
	}
	#endregion
}
