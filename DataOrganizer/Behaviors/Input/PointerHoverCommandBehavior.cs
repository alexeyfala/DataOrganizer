using Avalonia;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Executes a command once the pointer has rested over <see cref="Behavior{T}.AssociatedObject" />
/// for <see cref="Delay" /> milliseconds, ignoring pointer movements that only pass over it.
/// </summary>
internal sealed class PointerHoverCommandBehavior : Behavior<InputElement>
{
	#region Data
	/// <summary>
	/// Cancels the delay of the pointer resting over <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private CancellationTokenSource? _delay;
	#endregion

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
	/// Parameter of <see cref="Command" />, <see cref="Behavior{T}.AssociatedObject" /> when not set.
	/// </summary>
	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}

	/// <summary>
	/// Time in milliseconds the pointer has to rest over <see cref="Behavior{T}.AssociatedObject" />
	/// before the command runs.
	/// </summary>
	public int Delay
	{
		get => GetValue(DelayProperty);
		set => SetValue(DelayProperty, value);
	}
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
			UiConstants.TipDelay);
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerEntered" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerEntered(object? sender, PointerEventArgs e)
	{
		CancelDelay();

		_delay = new CancellationTokenSource();

		_ = ExecuteAfterDelayAsync(_delay.Token);
	}

	/// <summary>
	/// <see cref="InputElement.PointerExited" /> handler of <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerExited(object? sender, PointerEventArgs e) => CancelDelay();

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
	#endregion

	#region Helpers
	/// <summary>
	/// Drops the pending delay, leaving the command unexecuted.
	/// </summary>
	private void CancelDelay()
	{
		if (_delay is not { } source)
		{
			return;
		}

		_delay = null;

		source.Cancel();

		source.Dispose();
	}

	/// <summary>
	/// Executes <see cref="Command" /> when the pointer is still over <see cref="Behavior{T}.AssociatedObject" />
	/// after <see cref="Delay" />.
	/// </summary>
	private async Task ExecuteAfterDelayAsync(CancellationToken token)
	{
		try
		{
			await Task
				.Delay(Delay, token)
				.ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			return;
		}

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
}
