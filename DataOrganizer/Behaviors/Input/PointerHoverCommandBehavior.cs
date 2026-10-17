using Avalonia;
using Avalonia.Xaml.Interactivity;
using System.Windows.Input;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Executes a command once the pointer has rested over <see cref="Behavior{T}.AssociatedObject" />
/// for <see cref="PointerHoverBehaviorBase.Delay" /> milliseconds, ignoring pointer movements that only pass over it.
/// </summary>
internal sealed class PointerHoverCommandBehavior : PointerHoverBehaviorBase
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
	/// Parameter of <see cref="Command" />, <see cref="Behavior{T}.AssociatedObject" /> when not set.
	/// </summary>
	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="CommandParameter" />
	public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty
		.Register<PointerHoverCommandBehavior, object?>(nameof(CommandParameter));

	/// <inheritdoc cref="Command" />
	public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty
		.Register<PointerHoverCommandBehavior, ICommand?>(nameof(Command));
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnHover()
	{
		if (Command is not { } command)
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
