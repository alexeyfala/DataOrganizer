using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using System.Windows.Input;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Executes a command when the user expands or collapses the attached <see cref="Expander" /> or one inside it, but not
/// when code does.
/// </summary>
internal sealed class ExpanderToggledByUserBehavior : Behavior<Expander>
{
	#region Properties
	/// <summary>
	/// Command to execute when the user expands or collapses an expander.
	/// </summary>
	public ICommand? Command
	{
		get => GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="Command" />
	public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty
		.Register<ExpanderToggledByUserBehavior, ICommand?>(nameof(Command));
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Expander.ExpandedEvent" /> and <see cref="Expander.CollapsedEvent" /> handler of
	/// <see cref="Behavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Toggled(object? sender, RoutedEventArgs e)
	{
		// Code changes the state with the pointer elsewhere, while the user toggles the expander under the pointer.
		if (e.Source is not Expander { IsPointerOver: true }
			|| Command is not { } command
			|| !command.CanExecute(null))
		{
			return;
		}

		// If this event is not handled, it may fire multiple times, especially in highly nested groups.
		e.Handled = true;

		command.Execute(null);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.AddHandler(Expander.ExpandedEvent, AssociatedObject_Toggled);

		AssociatedObject?.AddHandler(Expander.CollapsedEvent, AssociatedObject_Toggled);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.RemoveHandler(Expander.ExpandedEvent, AssociatedObject_Toggled);

		AssociatedObject?.RemoveHandler(Expander.CollapsedEvent, AssociatedObject_Toggled);
	}
	#endregion
}
