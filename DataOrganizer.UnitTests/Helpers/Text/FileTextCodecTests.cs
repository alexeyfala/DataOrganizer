using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
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
	/// <see cref="FileTextCodec.TryRead" />: bytes that are not text give no text: UTF-16 without its byte order mark.
	/// </summary>
	[Test]
	public void TryRead_Returns_Null_For_Bytes_That_Are_Not_Text()
	{
		// Act
		string? text = FileTextCodec.TryRead([0x48, 0x00, 0x69, 0x00]);

		// Assert
		text
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileTextCodec.TryRead" />: reads the text in the encoding that the bytes are found in, without the byte
	/// order mark.
	/// </summary>
	[TestCase(65001)]
	[TestCase(1201)]
	public void TryRead_Returns_The_Text_In_The_Encoding_It_Finds(int codePage)
	{
		// Arrange
		Encoding unicode = Encoding.GetEncoding(codePage);

		byte[] contents = [.. unicode.GetPreamble(), .. unicode.GetBytes(CyrillicText)];

		// Act
		string? text = FileTextCodec.TryRead(contents);

		// Assert
		text
			.Should()
			.Be(CyrillicText);
	}
	#endregion
}
