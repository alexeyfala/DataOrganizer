using AwesomeAssertions;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using System.Text;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentEditorViewModel)}"" type")]
internal class DocumentEditorViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="DocumentEditorViewModel.FindUnreadableEncodings" />: the byte order mark of the bytes counts, so ASCII
	/// cannot read ASCII letters stored with the mark of UTF-8.
	/// </summary>
	[Test]
	public void FindUnreadableEncodings_Counts_The_Byte_Order_Mark()
	{
		// Arrange
		DocumentEditorViewModel sut = DocumentEditorFactory.CreateEditor(
			[.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("Hello")]);

		// Act
		sut.FindUnreadableEncodings();

		// Assert
		sut.UnreadableEncodings
			.Should()
			.Contain(Encoding.ASCII.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FindUnreadableEncodings" />: a text with a character that its encoding lacks has
	/// no bytes to read, so no encoding is found unreadable.
	/// </summary>
	[Test]
	public void FindUnreadableEncodings_Finds_None_While_The_Text_Does_Not_Fit_The_Encoding()
	{
		// Arrange
		// Not UTF-8, so in the fallback code page, which has no emoji
		DocumentEditorViewModel sut = DocumentEditorFactory.CreateEditor([0xC0, 0xC1, 0xC2]);

		sut.FindUnreadableEncodings();

		sut.Document.Text += "😀";

		// Act
		sut.FindUnreadableEncodings();

		// Assert
		sut.UnreadableEncodings
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FindUnreadableEncodings" />: the encodings that cannot read the bytes of the text
	/// are found, here UTF-16 for an odd number of bytes.
	/// </summary>
	[Test]
	public void FindUnreadableEncodings_Finds_The_Encodings_That_Cannot_Read_The_Text()
	{
		// Arrange
		DocumentEditorViewModel sut = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Hi!"),
			Encoding.UTF8.WebName);

		// Act
		sut.FindUnreadableEncodings();

		// Assert
		sut.UnreadableEncodings
			.Should()
			.Contain(Encoding.Unicode.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FindUnreadableEncodings" />: the text counts as it is now, before an edit is
	/// written, so a letter typed outside ASCII leaves ASCII unable to read it.
	/// </summary>
	[Test]
	public void FindUnreadableEncodings_Reads_The_Text_As_It_Is_Now()
	{
		// Arrange
		DocumentEditorViewModel sut = DocumentEditorFactory.CreateEditor(Encoding.UTF8.GetBytes("Hello"));

		sut.Document.Text = "Привет";

		// Act
		sut.FindUnreadableEncodings();

		// Assert
		sut.UnreadableEncodings
			.Should()
			.Contain(Encoding.ASCII.WebName)
			.And
			.NotContain(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.RefreshEncoding" />: the editor takes the encoding its text is read in, with its
	/// name, and the one found from its bytes.
	/// </summary>
	[Test]
	public void RefreshEncoding_Takes_The_Encodings_Of_The_Text()
	{
		// Arrange
		DocumentEditorViewModel sut = new();

		// Bytes of UTF-8, read in UTF-16 by choice.
		sut.Codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.Unicode.WebName);

		// Act
		sut.RefreshEncoding();

		// Assert
		sut
			.Should()
			.BeEquivalentTo(new
			{
				DefaultEncoding = Encoding.UTF8.WebName,
				Encoding = Encoding.Unicode.WebName,
				EncodingName = "UTF-16 LE"
			});
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ReplaceText" />: the new text is not an edit, so undo starts from it.
	/// </summary>
	[Test]
	public void ReplaceText_Starts_The_Undo_From_The_Text()
	{
		// Arrange
		DocumentEditorViewModel sut = new();

		sut.Document.Text = "Edit";

		// Act
		sut.ReplaceText("Text");

		// Assert
		sut.Document.UndoStack.CanUndo
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ReplaceText" />: the editor takes the encoding the new text is read in, with its
	/// name, and the one found from its bytes.
	/// </summary>
	[Test]
	public void ReplaceText_Takes_The_Encodings_Of_The_Text()
	{
		// Arrange
		DocumentEditorViewModel sut = new();

		// Bytes of UTF-8, read in UTF-16 by choice.
		string text = sut.Codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.Unicode.WebName)!;

		// Act
		sut.ReplaceText(text);

		// Assert
		sut
			.Should()
			.BeEquivalentTo(new
			{
				DefaultEncoding = Encoding.UTF8.WebName,
				Encoding = Encoding.Unicode.WebName,
				EncodingName = "UTF-16 LE"
			});
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SplitShare" />: a text split for the first time is split in halves.
	/// </summary>
	[Test]
	public void SplitShare_Starts_At_Half()
	{
		// Act
		DocumentEditorViewModel sut = new();

		// Assert
		sut.SplitShare
			.Should()
			.Be(0.5);
	}
	#endregion
}
