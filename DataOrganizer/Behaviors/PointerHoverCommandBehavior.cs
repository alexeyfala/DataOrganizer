using Avalonia;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Shared.Common;
using System.Threading.Tasks;
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

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerEntered" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerEntered(object? sender, PointerEventArgs e) => _ = ExecuteAfterDelayAsync();
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
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Executes <see cref="Command" /> when the pointer is still over <see cref="AssociatedObject" />
	/// after <see cref="Delay" />.
	/// </summary>
	private async Task ExecuteAfterDelayAsync()
	{
		await Task
			.Delay(Delay)
			.ConfigureAwait(true);

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
