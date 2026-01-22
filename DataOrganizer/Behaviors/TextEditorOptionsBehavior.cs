using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

namespace DataOrganizer.Behaviors;

internal sealed class TextEditorOptionsBehavior : Behavior<TextEditor>
{
	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject
			.Options
			.AllowScrollBelowDocument = false;
	}
	#endregion
}
