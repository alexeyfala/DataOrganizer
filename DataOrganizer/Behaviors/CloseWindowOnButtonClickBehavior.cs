using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Closes the attached <see cref="Window" /> when any nested <see cref="Button" />
/// inside it raises <see cref="Button.ClickEvent" />.
/// </summary>
internal sealed class CloseWindowOnButtonClickBehavior : Behavior<Window>
{
	#region Event Handlers
	/// <summary>
	/// Bubbled <see cref="Button.ClickEvent" /> handler.
	/// </summary>
	private void AssociatedObject_ButtonClick(object? sender, RoutedEventArgs e) => AssociatedObject?.Close();
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.AddHandler(Button.ClickEvent, AssociatedObject_ButtonClick);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.RemoveHandler(Button.ClickEvent, AssociatedObject_ButtonClick);
	}
	#endregion
}
