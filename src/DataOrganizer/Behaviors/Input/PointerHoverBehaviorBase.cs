using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Helpers.Views;
using System;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Acts once the pointer has rested over <see cref="Behavior{T}.AssociatedObject" /> for <see cref="Delay" />
/// milliseconds, ignoring pointer movements that only pass over it.
/// </summary>
internal abstract class PointerHoverBehaviorBase : Behavior<InputElement>
{
	#region Data
	/// <summary>
	/// Cancels the pending action.
	/// </summary>
	private IDisposable? _delay;
	#endregion

	#region Properties
	/// <summary>
	/// Time in milliseconds the pointer has to rest over <see cref="Behavior{T}.AssociatedObject" />
	/// before the action runs.
	/// </summary>
	public int Delay
	{
		get => GetValue(DelayProperty);
		set => SetValue(DelayProperty, value);
	}

	/// <summary>
	/// Runs an action once after a delay, <see cref="DispatcherTimer.RunOnce" /> by default; disposing the result cancels it.
	/// </summary>
	internal Func<System.Action, TimeSpan, IDisposable> RunOnce { get; init; } =
		static (action, delay) => DispatcherTimer.RunOnce(action, delay);
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="Delay" />
	public static readonly StyledProperty<int> DelayProperty = AvaloniaProperty
		.Register<PointerHoverBehaviorBase, int>(
			nameof(Delay),
			UiConstants.TipDelay);
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerEntered" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerEntered(object? sender, PointerEventArgs e)
	{
		CancelDelay();

		_delay = RunOnce(HoverIfPointerOver, TimeSpan.FromMilliseconds(Delay));
	}

	/// <summary>
	/// <see cref="InputElement.PointerExited" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerExited(object? sender, PointerEventArgs e)
	{
		CancelDelay();

		OnPointerExited();
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressed" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(object? sender, PointerPressedEventArgs e) => CancelDelay();
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

		AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;

		AssociatedObject.PointerExited += AssociatedObject_PointerExited;

		AssociatedObject.PointerPressed += AssociatedObject_PointerPressed;
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		CancelDelay();

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;

		AssociatedObject.PointerExited -= AssociatedObject_PointerExited;

		AssociatedObject.PointerPressed -= AssociatedObject_PointerPressed;
	}

	/// <summary>
	/// Runs when the pointer is still over <see cref="Behavior{T}.AssociatedObject" /> at the end of <see cref="Delay" />.
	/// </summary>
	protected abstract void OnHover();

	/// <summary>
	/// Runs when the pointer leaves <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	protected virtual void OnPointerExited()
	{
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Drops the pending delay, leaving the action undone.
	/// </summary>
	private void CancelDelay()
	{
		_delay?.Dispose();

		_delay = null;
	}

	/// <summary>
	/// Runs <see cref="OnHover" /> when the pointer is still over <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void HoverIfPointerOver()
	{
		_delay = null;

		if (AssociatedObject is not { IsPointerOver: true })
		{
			return;
		}

		OnHover();
	}
	#endregion
}
