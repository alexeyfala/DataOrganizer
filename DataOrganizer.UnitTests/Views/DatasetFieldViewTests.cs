using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Clipboard;
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
	#endregion
}
