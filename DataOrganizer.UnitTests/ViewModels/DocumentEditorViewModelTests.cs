using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using EditorDocuments.Interfaces;
using NSubstitute;
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

		sut.Document.AsTextDocument().Text += "😀";

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

		sut.Document.AsTextDocument().Text = "Привет";

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
		using AutoMock mock = AutoMock.GetLoose();

		DocumentEditorViewModel sut = mock.Create<DocumentEditorViewModel>();

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
	/// <see cref="DocumentEditorViewModel.ReplaceText" />: the document takes the new text as one that is not an edit.
	/// </summary>
	[Test]
	public void ReplaceText_Replaces_The_Text_Of_The_Document()
	{
		// Arrange
		IDocumentText document = Substitute.For<IDocumentText>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(document));

		DocumentEditorViewModel sut = mock.Create<DocumentEditorViewModel>();

		// Act
		sut.ReplaceText("Text");

		// Assert
		document
			.Received(1)
			.Replace("Text");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ReplaceText" />: the editor takes the encoding the new text is read in, with its
	/// name, and the one found from its bytes.
	/// </summary>
	[Test]
	public void ReplaceText_Takes_The_Encodings_Of_The_Text()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		DocumentEditorViewModel sut = mock.Create<DocumentEditorViewModel>();

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
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		DocumentEditorViewModel sut = mock.Create<DocumentEditorViewModel>();

		// Assert
		sut.SplitShare
			.Should()
			.Be(0.5);
	}
	#endregion
}
