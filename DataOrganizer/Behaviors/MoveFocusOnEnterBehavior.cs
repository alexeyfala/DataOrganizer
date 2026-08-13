using Avalonia;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Moves the focus from the attached <see cref="InputElement" /> to <see cref="Target" />
/// when <see cref="Key.Enter" /> is pressed, so the key does not reach the default button.
/// </summary>
internal sealed class MoveFocusOnEnterBehavior : Behavior<InputElement>
{
	#region Properties
	/// <summary>
	/// When <see langword="false" />, Enter is left to the default handling.
	/// </summary>
	public bool IsMoveAllowed
	{
		get => GetValue(IsMoveAllowedProperty);
		set => SetValue(IsMoveAllowedProperty, value);
	}

	/// <summary>
	/// Control the focus is moved to; a hidden or disabled one is left alone.
	/// </summary>
	public InputElement? Target
	{
		get => GetValue(TargetProperty);
		set => SetValue(TargetProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="IsMoveAllowed" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsMoveAllowedProperty = AvaloniaProperty
		.Register<MoveFocusOnEnterBehavior, bool>(name: nameof(IsMoveAllowed));

	/// <summary>
	/// Identifies the <see cref="Target" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<InputElement?> TargetProperty = AvaloniaProperty
		.Register<MoveFocusOnEnterBehavior, InputElement?>(name: nameof(Target));
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.KeyDownEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter
			|| !IsMoveAllowed
			|| Target is not { IsVisible: true, IsEffectivelyEnabled: true } target)
		{
			return;
		}

		target.Focus(NavigationMethod.Tab);

		e.Handled = true;
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
