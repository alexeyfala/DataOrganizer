using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Closes <see cref="Window" /> after the attached <see cref="Button" /> has been clicked.
/// </summary>
internal sealed class CloseWindowOnClickBehavior : Behavior<Button>
{
	#region Properties
	/// <summary>
	/// Target window to close on click.
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
		.Register<CloseWindowOnClickBehavior, Window?>(name: nameof(Window));
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Button.ClickEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Click(object? sender, RoutedEventArgs e)
	{
		if (Window is not { } window)
		{
			return;
		}

		// Defer to the next dispatcher pass so Button.Command has already run.
		Dispatcher
			.UIThread
			.Post(window.Close);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.AddHandler(Button.ClickEvent, AssociatedObject_Click);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.RemoveHandler(Button.ClickEvent, AssociatedObject_Click);
	}
	#endregion
}
