using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using System.Collections.Generic;
using System.Text;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentCodec)}"" type")]
internal class DocumentCodecTests
{
	#region Data
	/// <summary>
	/// Text that every Cyrillic encoding and every Unicode one holds.
	/// </summary>
	private const string CyrillicText = "Привет, мир";

	/// <summary>
	/// Web name of the Cyrillic code page of DOS, which reads every byte and has no emoji.
	/// </summary>
	private const string DosCyrillic = "cp866";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DocumentCodec.Encode" />: gives no bytes before any have been read, as the text has no encoding yet.
	/// </summary>
	[Test]
	public void Encode_Gives_Nothing_Before_The_Bytes_Are_Read()
	{
		// Arrange
		DocumentCodec sut = new();

		// Act
		byte[]? encoded = sut.Encode(CyrillicText, out string? missingCharacter);

		// Assert
		encoded
			.Should()
			.BeNull();

		missingCharacter
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentCodec.Encode" />: gives the text the bytes it was read from, with their byte order mark.
	/// </summary>
	[TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0xD0, 0x9F, 0x21 })]
	[TestCase(new byte[] { 0xFE, 0xFF, 0x04, 0x1F, 0x00, 0x21 })]
	[TestCase(new byte[] { 0xD0, 0x9F, 0x21 })]
	public void Encode_Gives_The_Bytes_The_Text_Was_Read_From(byte[] contents)
	{
		// Arrange
		DocumentCodec sut = new();

		string text = sut.Read(contents, chosenEncoding: null)!;

		// Act
		byte[]? encoded = sut.Encode(text, out _);

		// Assert
		encoded
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="DocumentCodec.Encode" />: a character that the encoding does not have leaves the text without bytes,
	/// comes back, and marks the text as outside the encoding.
	/// </summary>
	[Test]
	public void Encode_Refuses_A_Character_Outside_The_Encoding()
	{
		// Arrange
		DocumentCodec sut = new();

		sut.Read(Encoding.UTF8.GetBytes("Hi"), DosCyrillic);

		// Act
		byte[]? encoded = sut.Encode("Hi 😀", out string? missingCharacter);

		// Assert
		encoded
			.Should()
			.BeNull();

		missingCharacter
			.Should()
			.Be("😀");

		sut.IsTextOutsideEncoding
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentCodec.Encode" />: a text that fits the encoding again takes the mark of a text outside it off.
	/// </summary>
	[Test]
	public void Encode_Takes_The_Mark_Off_A_Text_That_Fits_Again()
	{
		// Arrange
		DocumentCodec sut = new();

		sut.Read(Encoding.UTF8.GetBytes("Hi"), DosCyrillic);

		sut.Encode("Hi 😀", out _);

		// Act
		byte[]? encoded = sut.Encode("Hi", out _);

		// Assert
		encoded
			.Should()
			.Equal(Encoding.ASCII.GetBytes("Hi"));

		sut.IsTextOutsideEncoding
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentCodec.FindUnreadableEncodings" />: a text without bytes in its encoding, as before the bytes are
	/// read or with a character outside the encoding, has nothing to read.
	/// </summary>
	[TestCase(null)]
	[TestCase(DosCyrillic)]
	public void FindUnreadableEncodings_Finds_Nothing_For_A_Text_Without_Bytes(string? readEncoding)
	{
		// Arrange
		DocumentCodec sut = new();

		// Without an encoding nothing is read.
		if (readEncoding is not null)
		{
			sut.Read(Encoding.UTF8.GetBytes("Hi"), readEncoding);
		}

		// Act
		IReadOnlySet<string>? unreadable = sut.FindUnreadableEncodings("Hi 😀");

		// Assert
		unreadable
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentCodec.FindUnreadableEncodings" />: finds the encodings that cannot read the bytes of the text as it
	/// is now, not those of the bytes it was read from.
	/// </summary>
	[Test]
	public void FindUnreadableEncodings_Finds_The_Encodings_That_Cannot_Read_The_Text()
	{
		// Arrange
		DocumentCodec sut = new();

		sut.Read(Encoding.UTF8.GetBytes("Hi"), chosenEncoding: null);

		// Act
		IReadOnlySet<string>? unreadable = sut.FindUnreadableEncodings(CyrillicText);

		// Assert
		IReadOnlySet<string> expected = FileTextCodec.FindUnreadableEncodings(Encoding.UTF8.GetBytes(CyrillicText));

		unreadable
			.Should()
			.BeEquivalentTo(expected);
	}

	/// <summary>
	/// <see cref="DocumentCodec.Read" />: the encoding found from the bytes becomes the default one, whatever encoding reads
	/// them.
	/// </summary>
	[Test]
	public void Read_Keeps_The_Encoding_Found_From_The_Bytes_As_The_Default()
	{
		// Arrange
		DocumentCodec sut = new();

		// Act
		sut.Read(Encoding.UTF8.GetBytes("Hi"), DosCyrillic);

		// Assert
		sut.Encoding
			.Should()
			.Be(DosCyrillic);

		sut.DefaultEncoding
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="DocumentCodec.Read" />: reads the text in the chosen encoding and names it as the status bar does.
	/// </summary>
	[Test]
	public void Read_Reads_The_Text_In_The_Chosen_Encoding()
	{
		// Arrange
		DocumentCodec sut = new();

		byte[] contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(CyrillicText);

		// Act
		string? text = sut.Read(contents, DosCyrillic);

		// Assert
		text
			.Should()
			.Be(CyrillicText);

		sut.EncodingName
			.Should()
			.Be("CP866");
	}

	/// <summary>
	/// <see cref="DocumentCodec.Read" />: bytes that are not text give no text and no encoding: the start of a PNG image.
	/// </summary>
	[Test]
	public void Read_Returns_Null_For_Bytes_That_Are_Not_Text()
	{
		// Arrange
		DocumentCodec sut = new();

		// Act
		string? text = sut.Read(
			[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
			chosenEncoding: null);

		// Assert
		text
			.Should()
			.BeNull();

		sut.Encoding
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentCodec.Read" />: a choice that cannot read the bytes leaves them to the encoding found from them:
	/// here UTF-16 for an odd number of bytes.
	/// </summary>
	[Test]
	public void Read_Takes_The_Found_Encoding_When_The_Choice_Cannot_Read()
	{
		// Arrange
		DocumentCodec sut = new();

		// Act
		string? text = sut.Read(Encoding.UTF8.GetBytes("Hi!"), "utf-16");

		// Assert
		text
			.Should()
			.Be("Hi!");

		sut.Encoding
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="DocumentCodec.Reread" />: a text without bytes in its encoding, as before the bytes are read or with a
	/// character outside the encoding, cannot be read again.
	/// </summary>
	[TestCase(null)]
	[TestCase(DosCyrillic)]
	public void Reread_Gives_Nothing_For_A_Text_Without_Bytes(string? readEncoding)
	{
		// Arrange
		DocumentCodec sut = new();

		// Without an encoding nothing is read.
		if (readEncoding is not null)
		{
			sut.Read(Encoding.UTF8.GetBytes("Hi"), readEncoding);
		}

		// Act
		EncodingChange? change = sut.Reread("Hi 😀", Encoding.UTF8.WebName);

		// Assert
		change
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentCodec.Reread" />: an encoding that cannot read the bytes gives no text and is named, while the text
	/// keeps its encoding: here UTF-16 for an odd number of bytes, and a name the list does not hold.
	/// </summary>
	[TestCase("utf-16", "UTF-16 LE")]
	[TestCase("no-such-encoding", "no-such-encoding")]
	public void Reread_Keeps_The_Encoding_When_The_Choice_Cannot_Read(string chosenEncoding, string expected)
	{
		// Arrange
		DocumentCodec sut = new();

		sut.Read(Encoding.UTF8.GetBytes("Hi!"), chosenEncoding: null);

		// Act
		EncodingChange? change = sut.Reread("Hi!", chosenEncoding);

		// Assert
		change!.Text
			.Should()
			.BeNull();

		change.Name
			.Should()
			.Be(expected);

		sut.Encoding
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="DocumentCodec.Reread" />: reads the same bytes in the chosen encoding, which the text takes, so they stay the
	/// bytes of the text read again.
	/// </summary>
	[Test]
	public void Reread_Reads_The_Same_Bytes_In_The_Chosen_Encoding()
	{
		// Arrange
		DocumentCodec sut = new();

		byte[] contents = Encoding.UTF8.GetBytes(CyrillicText);

		sut.Read(contents, chosenEncoding: null);

		// Act
		EncodingChange? change = sut.Reread(CyrillicText, DosCyrillic);

		// Assert
		string expected = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetString(contents);

		change!.Text
			.Should()
			.Be(expected);

		change.Name
			.Should()
			.Be("CP866");

		sut.Encode(expected, out _)
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="DocumentCodec.StoredEncoding" />: the encoding found from the bytes needs no choice in the editor state.
	/// </summary>
	[Test]
	public void StoredEncoding_Is_Null_For_The_Found_Encoding()
	{
		// Arrange
		DocumentCodec sut = new();

		// Act
		sut.Read(Encoding.UTF8.GetBytes("Hi"), Encoding.UTF8.WebName);

		// Assert
		sut.StoredEncoding
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentCodec.StoredEncoding" />: an encoding other than the found one is kept as a choice.
	/// </summary>
	[Test]
	public void StoredEncoding_Names_A_Chosen_Encoding()
	{
		// Arrange
		DocumentCodec sut = new();

		// Act
		sut.Read(Encoding.UTF8.GetBytes("Hi"), DosCyrillic);

		// Assert
		sut.StoredEncoding
			.Should()
			.Be(DosCyrillic);
	}
	#endregion
}
