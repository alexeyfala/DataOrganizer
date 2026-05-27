using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Closes the attached <see cref="Window" /> when the user presses <see cref="Key.Escape" />.
/// </summary>
internal sealed class CloseWindowOnEscapeBehavior : Behavior<Window>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.KeyDown" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Escape)
		{
			return;
		}

		AssociatedObject?.Close();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.KeyDown += AssociatedObject_KeyDown;
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.KeyDown -= AssociatedObject_KeyDown;
	}
	#endregion
}
