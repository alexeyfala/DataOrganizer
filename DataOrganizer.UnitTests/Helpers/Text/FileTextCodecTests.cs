using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(FileTextCodec)}"" type")]
internal class FileTextCodecTests
{
	#region Data
	/// <summary>
	/// Text that every Cyrillic encoding and every Unicode one holds.
	/// </summary>
	private const string CyrillicText = "Привет, мир";

	/// <summary>
	/// Cyrillic code page of Windows, the fallback of the tests whatever the region of the machine.
	/// </summary>
	private static readonly Encoding Cyrillic = CodePagesEncodingProvider.Instance.GetEncoding(1251)!;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FileTextCodec.Choose" />: finds an encoding of the list by its web name, whatever its case.
	/// </summary>
	[TestCase("cp866")]
	[TestCase("CP866")]
	public void Choose_Finds_An_Encoding_By_Its_Web_Name(string name)
	{
		// Act
		FileEncoding? encoding = FileTextCodec.Choose(Cyrillic.GetBytes(CyrillicText), name);

		// Assert
		encoding!.Encoding.CodePage
			.Should()
			.Be(866);
	}

	/// <summary>
	/// <see cref="FileTextCodec.Choose" />: a name the list does not hold chooses nothing, even that of an encoding .NET
	/// has apart from the list.
	/// </summary>
	[TestCase("iso-2022-jp")]
	[TestCase("no-such-encoding")]
	public void Choose_Returns_Null_For_A_Name_The_List_Does_Not_Hold(string name)
	{
		// Act
		FileEncoding? encoding = FileTextCodec.Choose(Cyrillic.GetBytes(CyrillicText), name);

		// Assert
		encoding
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileTextCodec.Choose" />: the encoding notes the byte order mark when the bytes start with it, so UTF-8
	/// and UTF-8 with the mark are one choice.
	/// </summary>
	[Test]
	public void Choose_Takes_The_Byte_Order_Mark_From_The_Bytes([Values] bool hasByteOrderMark)
	{
		// Arrange
		byte[] contents = [.. hasByteOrderMark ? Encoding.UTF8.GetPreamble() : [], .. Encoding.UTF8.GetBytes(CyrillicText)];

		// Act
		FileEncoding? encoding = FileTextCodec.Choose(contents, "utf-8");

		// Assert
		encoding!.HasByteOrderMark
			.Should()
			.Be(hasByteOrderMark);
	}

	/// <summary>
	/// <see cref="FileTextCodec.Detect(System.ReadOnlySpan{byte}, Encoding)" />: a byte order mark tells the Unicode
	/// encoding of the bytes, and the mark of UTF-32 LE is not taken for that of UTF-16 LE.
	/// </summary>
	[TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 65001)]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x41, 0x00 }, 1200)]
	[TestCase(new byte[] { 0xFE, 0xFF, 0x00, 0x41 }, 1201)]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 }, 12000)]
	[TestCase(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41 }, 12001)]
	public void Detect_Finds_The_Encoding_By_Its_Byte_Order_Mark(byte[] contents, int codePage)
	{
		// Act
		FileEncoding? encoding = FileTextCodec.Detect(contents, Cyrillic);

		// Assert
		encoding!.Encoding.CodePage
			.Should()
			.Be(codePage);

		encoding.HasByteOrderMark
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FileTextCodec.Detect(System.ReadOnlySpan{byte}, Encoding)" />: bytes without a mark that hold a zero byte
	/// are not text: UTF-16 without its mark and the start of a PNG image.
	/// </summary>
	[TestCase(new byte[] { 0x48, 0x00, 0x69, 0x00 })]
	[TestCase(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D })]
	public void Detect_Refuses_Bytes_With_A_Zero_Byte(byte[] contents)
	{
		// Act
		FileEncoding? encoding = FileTextCodec.Detect(contents, Cyrillic);

		// Assert
		encoding
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileTextCodec.Detect(System.ReadOnlySpan{byte}, Encoding)" />: bytes that are not UTF-8 and hold
	/// a control character that no text has are not text, although the fallback would read them.
	/// </summary>
	[TestCase(new byte[] { 0xCF, 0xF0, 0x07 })]
	[TestCase(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x1F })]
	public void Detect_Refuses_Control_Characters_Outside_UTF8(byte[] contents)
	{
		// Act
		FileEncoding? encoding = FileTextCodec.Detect(contents, Cyrillic);

		// Assert
		encoding
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileTextCodec.Detect(System.ReadOnlySpan{byte}, Encoding)" />: a text that is not UTF-8 takes the
	/// fallback, with its tabs, line breaks, form feeds, end of file mark and terminal escapes.
	/// </summary>
	[TestCase(CyrillicText)]
	[TestCase("Табуляция\tи строки\r\nстраница\f\vконец \u001B[0m\u001A")]
	public void Detect_Takes_The_Fallback_For_A_Text_That_Is_Not_UTF8(string text)
	{
		// Act
		FileEncoding? encoding = FileTextCodec.Detect(Cyrillic.GetBytes(text), Cyrillic);

		// Assert
		encoding
			.Should()
			.Be(new FileEncoding
			{
				Encoding = Cyrillic,
				HasByteOrderMark = false
			});
	}

	/// <summary>
	/// <see cref="FileTextCodec.Detect(System.ReadOnlySpan{byte}, Encoding)" />: bytes without a mark that are UTF-8 take
	/// UTF-8, an empty file and control characters included.
	/// </summary>
	[TestCase("")]
	[TestCase("Plain ASCII")]
	[TestCase(CyrillicText)]
	[TestCase("A bell \u0007 rings in UTF-8")]
	public void Detect_Takes_UTF8_For_Bytes_Without_A_Mark(string text)
	{
		// Act
		FileEncoding? encoding = FileTextCodec.Detect(Encoding.UTF8.GetBytes(text), Cyrillic);

		// Assert
		encoding
			.Should()
			.Be(new FileEncoding
			{
				Encoding = Encoding.UTF8,
				HasByteOrderMark = false
			});
	}

	/// <summary>
	/// <see cref="FileTextCodec.EncodingChoices" />: an encoding is found by its code page and by its web name.
	/// </summary>
	[Test]
	public void EncodingChoices_Are_Found_By_Code_Page_And_Web_Name()
	{
		// Act
		SelectorChoice choice = FileTextCodec
			.EncodingChoices
			.Single(static x => x.Id == "cp866");

		// Assert
		choice.SearchTerms
			.Should()
			.Equal("866", "cp866");
	}

	/// <summary>
	/// <see cref="FileTextCodec.EncodingChoices" />: the list holds every encoding of .NET and of the code pages that come
	/// with it, each once, and each id chooses its encoding.
	/// </summary>
	[Test]
	public void EncodingChoices_Hold_Each_Encoding_Once()
	{
		// Act
		string?[] ids = [.. FileTextCodec.EncodingChoices.Select(static x => x.Id)];

		// Assert
		// The number of .NET 10, which a new version of .NET may change.
		ids
			.Should()
			.HaveCount(116)
			.And
			.OnlyHaveUniqueItems()
			.And
			.OnlyContain(x => FileTextCodec.Choose(Encoding.UTF8.GetBytes(CyrillicText), x!) != null);
	}

	/// <summary>
	/// <see cref="FileTextCodec.EncodingChoices" />: the Unicode encodings come first, UTF-8 at their head, each once
	/// and named without a byte order mark.
	/// </summary>
	[Test]
	public void EncodingChoices_Put_Unicode_First()
	{
		// Act
		string[] names = [.. FileTextCodec.EncodingChoices.Take(5).Select(static x => x.Name)];

		// Assert
		names
			.Should()
			.Equal("UTF-8", "UTF-16 LE", "UTF-16 BE", "UTF-32 LE", "UTF-32 BE");
	}

	/// <summary>
	/// <see cref="FileTextCodec.EncodingChoices" />: the encodings after Unicode are sorted by description, whatever its
	/// case.
	/// </summary>
	[Test]
	public void EncodingChoices_Sort_The_Others_By_Description()
	{
		// Act
		SelectorChoice[] others = [.. FileTextCodec.EncodingChoices.Skip(5)];

		// Assert
		others
			.Should()
			.BeInAscendingOrder(static x => x.Description, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// <see cref="FileTextCodec.Fallback" />: the fallback is a code page that reads what UTF-8 cannot, never UTF-8 itself.
	/// </summary>
	[Test]
	public void Fallback_Is_Not_UTF8()
	{
		// Act
		int codePage = FileTextCodec.Fallback.CodePage;

		// Assert
		codePage
			.Should()
			.NotBe(Encoding.UTF8.CodePage);
	}

	/// <summary>
	/// <see cref="FileTextCodec.FindUnreadableEncodings" />: an encoding is found unreadable exactly when it cannot read
	/// the whole bytes, although a single-byte one is judged by the values that occur.
	/// </summary>
	[TestCaseSource(nameof(SampleContents))]
	public void FindUnreadableEncodings_Agrees_With_Reading_In_Each_Encoding(byte[] contents)
	{
		// Act
		IReadOnlySet<string> unreadable = FileTextCodec.FindUnreadableEncodings(contents);

		// Assert
		string[] expected = [.. FileTextCodec
			.EncodingChoices
			.Select(static x => x.Id!)
			.Where(x => FileTextCodec.Choose(contents, x) is not { } encoding
				|| FileTextCodec.TryDecode(contents, encoding) is null)];

		unreadable
			.Should()
			.BeEquivalentTo(expected);
	}

	/// <summary>
	/// <see cref="FileTextCodec.FindUnreadableEncodings" />: finds the encodings that cannot read a text of a code page,
	/// while another code page reads it, though not as it was written.
	/// </summary>
	[Test]
	public void FindUnreadableEncodings_Finds_The_Encodings_That_Cannot_Read_The_Bytes()
	{
		// Act
		IReadOnlySet<string> unreadable = FileTextCodec.FindUnreadableEncodings(Cyrillic.GetBytes(CyrillicText));

		// Assert
		unreadable
			.Should()
			.Contain(Encoding.UTF8.WebName)
			.And
			.Contain(Encoding.ASCII.WebName)
			.And
			.NotContain(Cyrillic.WebName)
			.And
			.NotContain("koi8-r");
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryDecode" />: reads the text of a code page.
	/// </summary>
	[Test]
	public void TryDecode_Reads_A_Code_Page()
	{
		// Arrange
		FileEncoding encoding = new()
		{
			Encoding = Cyrillic,
			HasByteOrderMark = false
		};

		// Act
		string? text = FileTextCodec.TryDecode(Cyrillic.GetBytes(CyrillicText), encoding);

		// Assert
		text
			.Should()
			.Be(CyrillicText);
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryDecode" />: bytes that lack the byte order mark of their encoding are not read.
	/// </summary>
	[Test]
	public void TryDecode_Refuses_A_Missing_Byte_Order_Mark()
	{
		// Arrange
		FileEncoding encoding = new()
		{
			Encoding = Encoding.Unicode,
			HasByteOrderMark = true
		};

		// Act
		string? text = FileTextCodec.TryDecode(Encoding.Unicode.GetBytes(CyrillicText), encoding);

		// Assert
		text
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryDecode" />: bytes that an encoding reads but would write back otherwise are not read, so
	/// a save cannot change them: here the shifts of ISO-2022-JP with nothing between them.
	/// </summary>
	[Test]
	public void TryDecode_Refuses_Bytes_It_Would_Not_Write_Back_The_Same()
	{
		// Arrange
		FileEncoding encoding = new()
		{
			Encoding = CodePagesEncodingProvider.Instance.GetEncoding(50220)!,
			HasByteOrderMark = false
		};

		// Act
		string? text = FileTextCodec.TryDecode([0x41, 0x0E, 0x0F, 0x42], encoding);

		// Assert
		text
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryDecode" />: bytes that the encoding cannot read give no text rather than replacement
	/// characters: a lone lead byte of UTF-8 and UTF-16 cut in the middle of a character.
	/// </summary>
	[TestCase(new byte[] { 0xC0, 0x41 }, 65001, false)]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x41 }, 1200, true)]
	public void TryDecode_Refuses_Bytes_The_Encoding_Cannot_Read(byte[] contents, int codePage, bool hasByteOrderMark)
	{
		// Arrange
		FileEncoding encoding = new()
		{
			Encoding = Encoding.GetEncoding(codePage),
			HasByteOrderMark = hasByteOrderMark
		};

		// Act
		string? text = FileTextCodec.TryDecode(contents, encoding);

		// Assert
		text
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryDecode" />: the byte order mark stays out of the text.
	/// </summary>
	[TestCase(65001)]
	[TestCase(1200)]
	[TestCase(1201)]
	[TestCase(12000)]
	public void TryDecode_Takes_The_Byte_Order_Mark_Off_The_Text(int codePage)
	{
		// Arrange
		Encoding unicode = Encoding.GetEncoding(codePage);

		FileEncoding encoding = new()
		{
			Encoding = unicode,
			HasByteOrderMark = true
		};

		byte[] contents = [.. unicode.GetPreamble(), .. unicode.GetBytes(CyrillicText)];

		// Act
		string? text = FileTextCodec.TryDecode(contents, encoding);

		// Assert
		text
			.Should()
			.Be(CyrillicText);
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryEncode" />: the byte order mark of the file goes back in front of the text.
	/// </summary>
	[TestCase(65001)]
	[TestCase(1201)]
	public void TryEncode_Puts_The_Byte_Order_Mark_Back(int codePage)
	{
		// Arrange
		Encoding unicode = Encoding.GetEncoding(codePage);

		FileEncoding encoding = new()
		{
			Encoding = unicode,
			HasByteOrderMark = true
		};

		// Act
		byte[]? contents = FileTextCodec.TryEncode(CyrillicText, encoding, out _);

		// Assert
		byte[] expected = [.. unicode.GetPreamble(), .. unicode.GetBytes(CyrillicText)];

		contents
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryEncode" />: a character that the encoding does not have stops the writing and comes back,
	/// rather than turning into a question mark or a look-alike.
	/// </summary>
	[TestCase("Привет 😀", "😀")]
	[TestCase("Кафе café", "é")]
	public void TryEncode_Refuses_A_Character_Outside_The_Encoding(string text, string expected)
	{
		// Arrange
		FileEncoding encoding = new()
		{
			Encoding = Cyrillic,
			HasByteOrderMark = false
		};

		// Act
		byte[]? contents = FileTextCodec.TryEncode(text, encoding, out string? missingCharacter);

		// Assert
		contents
			.Should()
			.BeNull();

		missingCharacter
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryEncode" />: writes the text in a code page.
	/// </summary>
	[Test]
	public void TryEncode_Writes_A_Code_Page()
	{
		// Arrange
		FileEncoding encoding = new()
		{
			Encoding = Cyrillic,
			HasByteOrderMark = false
		};

		// Act
		byte[]? contents = FileTextCodec.TryEncode(CyrillicText, encoding, out _);

		// Assert
		byte[] expected = Cyrillic.GetBytes(CyrillicText);

		contents
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryRead" />: a choice that cannot read the bytes leaves them to the encoding found from them:
	/// here UTF-16 for an odd number of bytes.
	/// </summary>
	[Test]
	public void TryRead_Falls_Back_On_The_Found_Encoding_When_The_Choice_Cannot_Read()
	{
		// Act
		FileText? read = FileTextCodec.TryRead(Encoding.UTF8.GetBytes("Hi!"), "utf-16");

		// Assert
		read!.Text
			.Should()
			.Be("Hi!");

		read.Encoding.Encoding.CodePage
			.Should()
			.Be(Encoding.UTF8.CodePage);
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryRead" />: the choice goes first, so it reads bytes that are not found to be text: here
	/// UTF-16 without its byte order mark.
	/// </summary>
	[Test]
	public void TryRead_Reads_In_The_Chosen_Encoding_What_The_Detection_Refuses()
	{
		// Act
		FileText? read = FileTextCodec.TryRead([0x48, 0x00, 0x69, 0x00], "utf-16");

		// Assert
		read!.Text
			.Should()
			.Be("Hi");
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryRead" />: reads the text in the chosen encoding rather than in the one found from the
	/// bytes.
	/// </summary>
	[Test]
	public void TryRead_Reads_The_Text_In_The_Chosen_Encoding()
	{
		// Arrange
		byte[] contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(CyrillicText);

		// Act
		FileText? read = FileTextCodec.TryRead(contents, "cp866");

		// Assert
		read!.Text
			.Should()
			.Be(CyrillicText);

		read.Encoding.Encoding.CodePage
			.Should()
			.Be(866);
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryRead" />: bytes that are not text give no text: UTF-16 without its byte order mark.
	/// </summary>
	[Test]
	public void TryRead_Returns_Null_For_Bytes_That_Are_Not_Text()
	{
		// Act
		FileText? read = FileTextCodec.TryRead([0x48, 0x00, 0x69, 0x00], chosenEncoding: null);

		// Assert
		read
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryRead" />: without a choice reads the text in the encoding that the bytes are found in,
	/// without the byte order mark.
	/// </summary>
	[TestCase(65001)]
	[TestCase(1201)]
	public void TryRead_Returns_The_Text_In_The_Encoding_It_Finds(int codePage)
	{
		// Arrange
		Encoding unicode = Encoding.GetEncoding(codePage);

		byte[] contents = [.. unicode.GetPreamble(), .. unicode.GetBytes(CyrillicText)];

		// Act
		FileText? read = FileTextCodec.TryRead(contents, chosenEncoding: null);

		// Assert
		read!.Text
			.Should()
			.Be(CyrillicText);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Contents that different encodings can read: none at all, text in several encodings, every byte value, random bytes
	/// and the shifts of ISO-2022-JP.
	/// </summary>
	private static byte[][] SampleContents()
	{
		// A fixed seed, so that a failure repeats.
		byte[] random = new byte[4096];

		new Random(1).NextBytes(random);

		return
		[
			[],
			Encoding.ASCII.GetBytes("Hi!"),
			Encoding.UTF8.GetBytes(CyrillicText),
			Cyrillic.GetBytes(CyrillicText),
			CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(CyrillicText),
			[.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(CyrillicText)],
			[.. Enumerable.Range(0, 256).Select(static x => (byte)x)],
			random,
			[0x41, 0x0E, 0x0F, 0x42]
		];
	}
	#endregion
}
