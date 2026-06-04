using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Starts a native move-drag of <see cref="Window" /> when the attached control is pressed with the left mouse button.
/// </summary>
internal sealed class MoveWindowBehavior : Behavior<Control>
{
	#region Properties
	/// <summary>
	/// Target window to move.
	/// </summary>
	public Window? Window
	{
		get => GetValue(WindowProperty);
		set => SetValue(WindowProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Window" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<Window?> WindowProperty = AvaloniaProperty
		.Register<MoveWindowBehavior, Window?>(name: nameof(Window));
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (AssociatedObject is null || !e.GetCurrentPoint(AssociatedObject)
			.Properties
			.IsLeftButtonPressed)
		{
			return;
		}

		Window?.BeginMoveDrag(e);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.AddHandler(InputElement.PointerPressedEvent, AssociatedObject_PointerPressed);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.RemoveHandler(InputElement.PointerPressedEvent, AssociatedObject_PointerPressed);
	}
	#endregion
}
