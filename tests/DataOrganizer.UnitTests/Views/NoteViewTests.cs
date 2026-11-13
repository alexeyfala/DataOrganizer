using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Security;
using DataOrganizer.Views;
using Material.Icons.Avalonia;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(NoteView)}"" type")]
internal class NoteViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteView.IsLocked" />: the icon of the note shows to UI Automation as an image of the same name, locked or
	/// not, so a locked note is not taken for the lock of its object.
	/// </summary>
	[AvaloniaTest]
	public void IsLocked_Keeps_The_Name_Of_The_Note_Image()
	{
		// Arrange
		NoteView locked = new()
		{
			IsLocked = true
		};

		NoteView readable = new()
		{
			IsLocked = false
		};

		Window window = new()
		{
			Content = new StackPanel
			{
				Children =
				{
					locked,
					readable
				}
			}
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		MaterialIcon lockedIcon = locked
			.GetVisualDescendants()
			.OfType<MaterialIcon>()
			.Single(static x => x.IsEffectivelyVisible);

		MaterialIcon readableIcon = readable
			.GetVisualDescendants()
			.OfType<MaterialIcon>()
			.Single(static x => x.IsEffectivelyVisible);

		// Act
		AutomationPeer lockedPeer = ControlAutomationPeer.CreatePeerForElement(lockedIcon);

		AutomationPeer readablePeer = ControlAutomationPeer.CreatePeerForElement(readableIcon);

		// Assert
		lockedPeer.GetAutomationControlType()
			.Should()
			.Be(AutomationControlType.Image);

		readablePeer.GetAutomationControlType()
			.Should()
			.Be(AutomationControlType.Image);

		lockedPeer.GetName()
			.Should()
			.NotBeNullOrEmpty()
			.And
			.Be(readablePeer.GetName());
	}

	/// <summary>
	/// <see cref="NoteView.IsNoteOpen" />: a note open when the view leaves the tree is closed, so it does not open
	/// again when the view comes back.
	/// </summary>
	[AvaloniaTest]
	public void IsNoteOpen_Turns_Off_When_The_View_Leaves_The_Tree()
	{
		// Arrange
		NoteView sut = new()
		{
			Note = "Note"
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		sut
			.NotePointerEnteredCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		Popup popup = sut
			.GetLogicalDescendants()
			.OfType<Popup>()
			.Single();

		// Act
		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		window.Content = sut;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.IsNoteOpen
			.Should()
			.BeFalse();

		popup.IsOpen
			.Should()
			.BeFalse();
	}

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
		SelectableTextBlock textBlock = view.FindControl<SelectableTextBlock>("NoteText")!;

		return Interaction
			.GetBehaviors(textBlock)
			.OfType<SensitiveCopyBehavior>()
			.Single();
	}
	#endregion
}
