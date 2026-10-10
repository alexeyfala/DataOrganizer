using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Passes Enter and Escape to the default and cancel buttons inside a native popup: Avalonia routes the keys of a popup
/// past the visual root those buttons listen to.
/// </summary>
internal sealed class PopupDialogKeysBehavior : Behavior<InputElement>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.KeyDownEvent" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is not (Key.Enter or Key.Escape)
			|| TopLevel.GetTopLevel(AssociatedObject) is not PopupRoot popupRoot
			|| popupRoot.GetVisualParent() is not Interactive visualRoot)
		{
			return;
		}

		KeyEventArgs forwarded = new()
		{
			Key = e.Key,
			KeyDeviceType = e.KeyDeviceType,
			KeyModifiers = e.KeyModifiers,
			KeySymbol = e.KeySymbol,
			PhysicalKey = e.PhysicalKey,
			RoutedEvent = InputElement.KeyDownEvent,
			Source = e.Source
		};

		visualRoot.RaiseEvent(forwarded);

		e.Handled = forwarded.Handled;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.AddHandler(InputElement.KeyDownEvent, AssociatedObject_KeyDown);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.RemoveHandler(InputElement.KeyDownEvent, AssociatedObject_KeyDown);
	}
	#endregion
}
