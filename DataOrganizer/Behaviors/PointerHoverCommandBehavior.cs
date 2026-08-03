using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Shared.Common;
using System;
using System.Windows.Input;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Executes a command once the pointer has rested over <see cref="AssociatedObject" />
/// for <see cref="Delay" /> milliseconds, ignoring pointer movements that only pass over it.
/// </summary>
internal sealed class PointerHoverCommandBehavior : Behavior<InputElement>
{
	#region Properties
	/// <summary>
	/// Command to execute on hover.
	/// </summary>
	public ICommand? Command
	{
		get => GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	/// <summary>
	/// Parameter of <see cref="Command" />, <see cref="AssociatedObject" /> when not set.
	/// </summary>
	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}

	/// <summary>
	/// Time in milliseconds the pointer has to rest over <see cref="AssociatedObject" />
	/// before the command runs.
	/// </summary>
	public int Delay
	{
		get => GetValue(DelayProperty);
		set => SetValue(DelayProperty, value);
	}

	/*
	/// <summary>
	/// Time the pointer has to rest over <see cref="AssociatedObject" /> before the command runs.
	/// </summary>
	public TimeSpan Delay
	{
		get => GetValue(DelayProperty);
		set => SetValue(DelayProperty, value);
	}
	*/
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="CommandParameter" />
	public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty
		.Register<PointerHoverCommandBehavior, object?>(nameof(CommandParameter));

	/// <inheritdoc cref="Command" />
	public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty
		.Register<PointerHoverCommandBehavior, ICommand?>(nameof(Command));

	/// <inheritdoc cref="Delay" />
	public static readonly StyledProperty<int> DelayProperty = AvaloniaProperty
		.Register<PointerHoverCommandBehavior, int>(
			nameof(Delay),
			AppUtils.TipDelay);
	#endregion

	#region Data
	/// <summary>
	/// Subscription of the pending hover timer.
	/// </summary>
	private IDisposable? _timer;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerEntered" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerEntered(object? sender, PointerEventArgs e)
	{
		StopTimer();

		_timer = DispatcherTimer.RunOnce(Timer_Tick, TimeSpan.FromMilliseconds(Delay));

		// _timer = DispatcherTimer.RunOnce(Timer_Tick, Delay);
	}

	/// <summary>
	/// <see cref="InputElement.PointerExited" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerExited(object? sender, PointerEventArgs e) => StopTimer();

	/// <summary>
	/// Handler of the elapsed hover timer.
	/// </summary>
	private void Timer_Tick()
	{
		// The one-shot timer has already elapsed, so there is nothing left to stop.
		_timer = null;

		if (AssociatedObject is not { IsPointerOver: true } || Command is not { } command)
		{
			return;
		}

		object? parameter = CommandParameter ?? AssociatedObject;

		if (!command.CanExecute(parameter))
		{
			return;
		}

		command.Execute(parameter);
	}
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
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		StopTimer();

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;

		AssociatedObject.PointerExited -= AssociatedObject_PointerExited;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Cancels the pending hover timer.
	/// </summary>
	private void StopTimer()
	{
		_timer?.Dispose();

		_timer = null;
	}
	#endregion
}
