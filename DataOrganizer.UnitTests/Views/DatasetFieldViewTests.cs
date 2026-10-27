using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using Clipboard.Interfaces;
using DataOrganizer.Views;
using NSubstitute;
using Shared.Common;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetFieldView)}"" type")]
internal class DatasetFieldViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="DatasetFieldView.CopyToClipboardCommand" />: the text goes to the clipboard writer
	/// together with its sensitivity.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyToClipboardCommand_Passes_The_Text_And_Its_Sensitivity_To_The_Writer([Values] bool isSensitive)
	{
		// Arrange
		string text = RandomString.Create(16);

		IClipboardTextWriter writer = Substitute.For<IClipboardTextWriter>();

		DatasetFieldView sut = new()
		{
			ClipboardWriter = writer,
			IsSensitive = isSensitive,
			Text = text
		};

		// Act
		await sut
			.CopyToClipboardCommand
			.ExecuteAsync(null);

		// Assert
		await writer
			.Received(1)
			.WriteTextAsync(text, isSensitive);
	}

	/// <summary>
	/// <see cref="DatasetFieldView.CopyToClipboardCommand" />: a blank text never reaches the clipboard.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyToClipboardCommand_Skips_A_Blank_Text()
	{
		// Arrange
		IClipboardTextWriter writer = Substitute.For<IClipboardTextWriter>();

		DatasetFieldView sut = new()
		{
			ClipboardWriter = writer,
			Text = "   "
		};

		// Act
		await sut
			.CopyToClipboardCommand
			.ExecuteAsync(null);

		// Assert
		await writer
			.DidNotReceive()
			.WriteTextAsync(Arg.Any<string>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="DatasetFieldView.DisplayText" />: a hidden text shows the same mask whatever its length.
	/// </summary>
	[AvaloniaTest]
	public void DisplayText_Hides_The_Length_Of_A_Hidden_Text()
	{
		// Arrange
		DatasetFieldView sut = new()
		{
			IsHidden = true,
			Text = "a"
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		string? maskOfShortText = sut.DisplayText;

		// Act
		sut.Text = RandomString.Create(40);

		// Assert
		sut.DisplayText
			.Should()
			.Be(maskOfShortText)
			.And
			.NotBeNullOrEmpty();
	}

	/// <summary>
	/// <see cref="DatasetFieldView.DisplayText" />: the field is named for UI Automation by what it shows, the text or its
	/// mask.
	/// </summary>
	[AvaloniaTest]
	public void DisplayText_Names_The_Field_For_UI_Automation([Values] bool isHidden)
	{
		// Arrange
		DatasetFieldView sut = new()
		{
			IsHidden = isHidden,
			Text = RandomString.Create(16)
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		string name = ControlAutomationPeer
			.CreatePeerForElement(sut)
			.GetName();

		// Assert
		name
			.Should()
			.NotBeNullOrEmpty()
			.And
			.Be(sut.DisplayText);
	}

	/// <summary>
	/// <see cref="DatasetFieldView.DisplayText" />: an empty hidden text shows no mask.
	/// </summary>
	[AvaloniaTest]
	public void DisplayText_Shows_No_Mask_For_An_Empty_Text()
	{
		// Arrange
		DatasetFieldView sut = new()
		{
			IsHidden = true,
			Text = string.Empty
		};

		Window window = new()
		{
			Content = sut
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.DisplayText
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetFieldView.DisplayText" />: a text that is not hidden is shown as it is.
	/// </summary>
	[AvaloniaTest]
	public void DisplayText_Shows_The_Text_That_Is_Not_Hidden()
	{
		// Arrange
		string text = RandomString.Create(16);

		DatasetFieldView sut = new()
		{
			Text = text
		};

		Window window = new()
		{
			Content = sut
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.DisplayText
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="DatasetFieldView.IsHidden" />: the toggle keeps the value while the view is out of the tree.
	/// </summary>
	[AvaloniaTest]
	public void IsHidden_Keeps_The_Toggle_On_While_The_View_Is_Out_Of_The_Tree()
	{
		// Arrange
		DatasetFieldView sut = new()
		{
			IsHidden = true
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ToggleButton toggle = sut
			.GetVisualDescendants()
			.OfType<ToggleButton>()
			.Single();

		// Act
		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		// Assert
		toggle.IsChecked
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetFieldView.IsSensitive" />: no element inside a sensitive field gives its text to UI Automation, a
	/// plain text or a link.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Keeps_The_Text_Out_Of_The_Elements_Inside([Values] bool isHyperlink)
	{
		// Arrange
		string text = isHyperlink
			? $"https://example.com/{RandomString.Create(16)}"
			: RandomString.Create(16);

		DatasetFieldView sut = new()
		{
			IsSensitive = true,
			Text = text
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		string?[] names =
		[
			.. sut
				.GetVisualDescendants()
				.OfType<Control>()
				.Select(static x => ControlAutomationPeer
					.CreatePeerForElement(x)
					.GetName())
		];

		// Assert (the text shows on the screen).
		sut.DisplayText
			.Should()
			.Be(text);

		names
			.Should()
			.NotContain(x => x != null && x.Contains(text));
	}

	/// <summary>
	/// <see cref="DatasetFieldView.IsSensitive" />: a sensitive field carries none of its text in the name for UI Automation,
	/// and every sensitive field gets the same one.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Keeps_The_Text_Out_Of_The_Name()
	{
		// Arrange
		DatasetFieldView first = new()
		{
			IsSensitive = true,
			Text = RandomString.Create(16)
		};

		DatasetFieldView second = new()
		{
			IsSensitive = true,
			Text = RandomString.Create(16)
		};

		Window window = new()
		{
			Content = new StackPanel
			{
				Children =
				{
					first,
					second
				}
			}
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		string firstName = ControlAutomationPeer
			.CreatePeerForElement(first)
			.GetName();

		string secondName = ControlAutomationPeer
			.CreatePeerForElement(second)
			.GetName();

		// Assert
		firstName
			.Should()
			.NotBeNullOrEmpty()
			.And
			.NotContain(first.Text!);

		secondName
			.Should()
			.Be(firstName);
	}

	/// <summary>
	/// <see cref="DatasetFieldView.Note" />: a field without a note builds no note view.
	/// </summary>
	[AvaloniaTest]
	public void Note_Absent_Builds_No_Note_View()
	{
		// Arrange
		DatasetFieldView sut = new()
		{
			Text = RandomString.Create(16)
		};

		Window window = new()
		{
			Content = sut
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.GetVisualDescendants().OfType<NoteView>()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetFieldView.Note" />: a note set on a shown field builds the note view that holds it.
	/// </summary>
	[AvaloniaTest]
	public void Note_Set_Later_Builds_The_Note_View_With_It()
	{
		// Arrange
		string note = RandomString.Create(16);

		DatasetFieldView sut = new()
		{
			Text = RandomString.Create(16)
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.Note = note;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.GetVisualDescendants().OfType<NoteView>()
			.Should()
			.ContainSingle()
			.Which
			.Note
			.Should()
			.Be(note);
	}
	#endregion
}
