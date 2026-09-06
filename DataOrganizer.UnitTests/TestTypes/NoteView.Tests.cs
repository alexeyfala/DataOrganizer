using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors;
using DataOrganizer.Views;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteView)}"" type")]
internal class NoteViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteView.IsSensitive" />: the declaration reaches the behavior that writes the copy.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Reaches_The_Copy_Behavior([Values] bool isSensitive)
	{
		// Arrange
		NoteView view = new()
		{
			IsSensitive = isSensitive
		};

		// Act
		SensitiveCopyBehavior behavior = GetCopyBehavior(view);

		// Assert
		behavior.IsSensitive
			.Should()
			.Be(isSensitive);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the behavior attached to the text block of the note popup.
	/// </summary>
	private static SensitiveCopyBehavior GetCopyBehavior(NoteView view)
	{
		SelectableTextBlock textBlock = view.FindControl<SelectableTextBlock>("NoteTextBlock")!;

		return Interaction
			.GetBehaviors(textBlock)
			.OfType<SensitiveCopyBehavior>()
			.Single();
	}
	#endregion
}
