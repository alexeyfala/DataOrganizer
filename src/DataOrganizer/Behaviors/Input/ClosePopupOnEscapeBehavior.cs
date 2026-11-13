using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using System;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Closes the associated <see cref="Popup" /> on <see cref="Key.Escape" />, wherever the focus is in its window.
/// </summary>
internal sealed class ClosePopupOnEscapeBehavior : Behavior<Popup>
{
	#region Data
	/// <summary>
	/// Window whose keys are listened to while the popup is open.
	/// </summary>
	private TopLevel? _topLevel;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Popup.Closed" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Closed(object? sender, EventArgs e) => StopListening();

	/// <summary>
	/// <see cref="Popup.Opened" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Opened(object? sender, EventArgs e) => StartListening();

	/// <summary>
	/// <see cref="InputElement.KeyDownEvent" /> handler of the window the popup belongs to.
	/// </summary>
	private void TopLevel_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Escape || AssociatedObject is not { IsOpen: true } popup)
		{
			return;
		}

		popup.Close();

		e.Handled = true;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.Opened += AssociatedObject_Opened;

		AssociatedObject?.Closed += AssociatedObject_Closed;
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		StopListening();

		AssociatedObject?.Opened -= AssociatedObject_Opened;

		AssociatedObject?.Closed -= AssociatedObject_Closed;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Listens to the keys of the whole window: a popup opened on hover leaves the focus where it was.
	/// </summary>
	private void StartListening()
	{
		StopListening();

		_topLevel = TopLevel.GetTopLevel(AssociatedObject);

		_topLevel?.AddHandler(InputElement.KeyDownEvent, TopLevel_KeyDown, RoutingStrategies.Tunnel);
	}

	/// <summary>
	/// Stops listening to the keys of the window.
	/// </summary>
	private void StopListening()
	{
		_topLevel?.RemoveHandler(InputElement.KeyDownEvent, TopLevel_KeyDown);

		_topLevel = null;
	}
	#endregion
}
