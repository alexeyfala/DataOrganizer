using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors;
using DataOrganizer.Interfaces.Clipboard;
using NSubstitute;
using Shared.Common;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SensitiveCopyBehavior)}"" type")]
internal class SensitiveCopyBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="SensitiveCopyBehavior.IsSensitive" />: ordinary text keeps the built-in copy.
	/// </summary>
	[AvaloniaTest]
	public void Copy_Leaves_An_Ordinary_Control_Alone()
	{
		// Arrange
		(_, TextBox input, ISensitiveClipboardWriter writer) = CreateSetup(
			RandomString.Create(16),
			isSensitive: false);

		RoutedEventArgs args = new(TextBox.CopyingToClipboardEvent);

		// Act
		input.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		writer
			.DidNotReceive()
			.Write(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="SensitiveCopyBehavior.IsSensitive" />: without a selection there is nothing to write.
	/// </summary>
	[AvaloniaTest]
	public void Copy_Leaves_An_Unselected_Text_Alone()
	{
		// Arrange
		(_, TextBox input, ISensitiveClipboardWriter writer) = CreateSetup(
			RandomString.Create(16),
			isSensitive: true);

		input.ClearSelection();

		RoutedEventArgs args = new(TextBox.CopyingToClipboardEvent);

		// Act
		input.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		writer
			.DidNotReceive()
			.Write(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="SensitiveCopyBehavior.IsSensitive" />: the copy of a text block is written with the markers.
	/// </summary>
	[AvaloniaTest]
	public void Copy_Writes_The_Selection_Of_A_Text_Block()
	{
		// Arrange
		string text = RandomString.Create(16);

		SelectableTextBlock textBlock = new()
		{
			Text = text,
			SelectionStart = 0,
			SelectionEnd = text.Length
		};

		ISensitiveClipboardWriter writer = Substitute.For<ISensitiveClipboardWriter>();

		SensitiveCopyBehavior sut = new()
		{
			IsSensitive = true,
			Writer = writer
		};

		sut.Attach(textBlock);

		// Act
		textBlock.Copy();

		// Assert
		writer
			.Received(1)
			.Write(text);
	}

	/// <summary>
	/// <see cref="SensitiveCopyBehavior.IsSensitive" />: the copy of a text box is written with the markers.
	/// </summary>
	[AvaloniaTest]
	public void Copy_Writes_The_Selection_Of_A_Text_Box()
	{
		// Arrange
		string text = RandomString.Create(16);

		(_, TextBox input, ISensitiveClipboardWriter writer) = CreateSetup(text, isSensitive: true);

		RoutedEventArgs args = new(TextBox.CopyingToClipboardEvent);

		// Act
		input.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeTrue();

		writer
			.Received(1)
			.Write(text);
	}

	/// <summary>
	/// <see cref="SensitiveCopyBehavior.IsSensitive" />: the cut writes the selection and removes it.
	/// </summary>
	[AvaloniaTest]
	public void Cut_Removes_The_Written_Selection()
	{
		// Arrange
		(_, TextBox input, ISensitiveClipboardWriter writer) = CreateSetup("abcdef", isSensitive: true);

		input.SelectionEnd = 3;

		RoutedEventArgs args = new(TextBox.CuttingToClipboardEvent);

		// Act
		input.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeTrue();

		input.Text
			.Should()
			.Be("def");

		writer
			.Received(1)
			.Write("abc");
	}

	/// <summary>
	/// <see cref="SensitiveCopyBehavior.OnDetaching" />: a detached behavior leaves the control as it found it.
	/// </summary>
	[AvaloniaTest]
	public void Detaching_Stops_The_Interception()
	{
		// Arrange
		(SensitiveCopyBehavior sut, TextBox input, ISensitiveClipboardWriter writer) = CreateSetup(
			RandomString.Create(16),
			isSensitive: true);

		RoutedEventArgs args = new(TextBox.CopyingToClipboardEvent);

		// Act
		sut.Detach();

		input.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		writer
			.DidNotReceive()
			.Write(Arg.Any<string>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the behavior attached to a text box whose whole text is selected.
	/// </summary>
	private static (SensitiveCopyBehavior Sut, TextBox Input, ISensitiveClipboardWriter Writer) CreateSetup(
		string text,
		bool isSensitive)
	{
		TextBox input = new()
		{
			Text = text,
			SelectionStart = 0,
			SelectionEnd = text.Length
		};

		ISensitiveClipboardWriter writer = Substitute.For<ISensitiveClipboardWriter>();

		SensitiveCopyBehavior sut = new()
		{
			IsSensitive = isSensitive,
			Writer = writer
		};

		sut.Attach(input);

		return (sut, input, writer);
	}
	#endregion
}
