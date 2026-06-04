using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Starts a native resize-drag of <see cref="Window" /> towards <see cref="Edge" /> when the attached
/// control is pressed with the left mouse button.
/// </summary>
internal sealed class ResizeWindowBehavior : Behavior<Control>
{
	#region Properties
	/// <summary>
	/// Window edge (or corner) the resize-drag is started towards.
	/// </summary>
	public WindowEdge Edge
	{
		get => GetValue(EdgeProperty);
		set => SetValue(EdgeProperty, value);
	}

	/// <summary>
	/// Target window to resize.
	/// </summary>
	public Window? Window
	{
		get => GetValue(WindowProperty);
		set => SetValue(WindowProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Edge" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<WindowEdge> EdgeProperty = AvaloniaProperty
		.Register<ResizeWindowBehavior, WindowEdge>(name: nameof(Edge));

	/// <summary>
	/// Identifies the <see cref="Window" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<Window?> WindowProperty = AvaloniaProperty
		.Register<ResizeWindowBehavior, Window?>(name: nameof(Window));
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

		Window?.BeginResizeDrag(Edge, e);
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
