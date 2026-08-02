using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using System.Windows.Input;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Executes a command for the tab that was clicked with the middle mouse button,
/// passing the tab's data item as the command parameter.
/// </summary>
internal sealed class MiddleClickTabCommandBehavior : Behavior<TabControl>
{
	#region Properties
	/// <summary>
	/// Command to execute for the clicked tab.
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
		.Register<MiddleClickTabCommandBehavior, ICommand?>(nameof(Command));
	#endregion

	#region Data
	/// <summary>
	/// Tab the middle button was pressed on.
	/// </summary>
	private TabItem? _pressedTabItem;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		_pressedTabItem = e.GetCurrentPoint(AssociatedObject).Properties.PointerUpdateKind
			is PointerUpdateKind.MiddleButtonPressed
			? GetTabItem(e.Source)
			: null;
	}

	/// <summary>
	/// <see cref="InputElement.PointerReleasedEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerReleased(
		object? sender,
		PointerReleasedEventArgs e)
	{
		TabItem? pressedTabItem = _pressedTabItem;

		_pressedTabItem = null;

		// The press and the release both have to happen on the same tab.
		if (pressedTabItem is null
			|| e.InitialPressMouseButton is not MouseButton.Middle
			|| !ReferenceEquals(GetTabItem(e.Source), pressedTabItem))
		{
			return;
		}

		if (pressedTabItem.DataContext is not { } item
			|| Command is not { } command
			|| !command.CanExecute(item))
		{
			return;
		}

		command.Execute(item);

		e.Handled = true;
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

		AssociatedObject.AddHandler(
			InputElement.PointerPressedEvent,
			AssociatedObject_PointerPressed);

		AssociatedObject.AddHandler(
			InputElement.PointerReleasedEvent,
			AssociatedObject_PointerReleased);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		_pressedTabItem = null;

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.RemoveHandler(
			InputElement.PointerPressedEvent,
			AssociatedObject_PointerPressed);

		AssociatedObject.RemoveHandler(
			InputElement.PointerReleasedEvent,
			AssociatedObject_PointerReleased);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the tab of <see cref="AssociatedObject" /> the event source belongs to.
	/// </summary>
	private TabItem? GetTabItem(object? source)
	{
		if (source is not Visual visual
			|| visual.FindAncestorOfType<TabItem>(includeSelf: true) is not { } tabItem)
		{
			return null;
		}

		return ReferenceEquals(tabItem.FindAncestorOfType<TabControl>(), AssociatedObject)
			? tabItem
			: null;
	}
	#endregion
}
