using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Marks <see cref="Button.ClickEvent" /> as handled for the attached <see cref="Button" />,
/// preventing the routed event from reaching ancestor click handlers (e.g.
/// <see cref="CloseWindowOnButtonClickBehavior" /> on the parent window).
/// </summary>
internal sealed class StopButtonClickBubblingBehavior : Behavior<Button>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="Button.ClickEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Click(object? sender, RoutedEventArgs e) => e.Handled = true;
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
