using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using System.Text;

namespace DataOrganizer.UnitTests.Dto.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(FileEncoding)}"" type")]
internal class FileEncodingTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileEncoding.Name" />: a Unicode encoding is named by its byte order, with the byte order mark noted,
	/// as in Notepad++.
	/// </summary>
	[TestCase(65001, false, "UTF-8")]
	[TestCase(65001, true, "UTF-8-BOM")]
	[TestCase(1200, true, "UTF-16 LE BOM")]
	[TestCase(1201, false, "UTF-16 BE")]
	[TestCase(12000, true, "UTF-32 LE BOM")]
	[TestCase(12001, true, "UTF-32 BE BOM")]
	public void Name_Notes_The_Byte_Order_Mark_Of_A_Unicode_Encoding(int codePage, bool hasByteOrderMark, string expected)
	{
		// Arrange
		FileEncoding sut = new()
		{
			Encoding = Encoding.GetEncoding(codePage),
			HasByteOrderMark = hasByteOrderMark
		};

		// Act
		string name = sut.Name;

		// Assert
		name
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="FileEncoding.Name" />: a code page of Windows starts with a capital letter, and any other one is written
	/// in capitals.
	/// </summary>
	[TestCase(1251, "Windows-1251")]
	[TestCase(866, "CP866")]
	[TestCase(20866, "KOI8-R")]
	[TestCase(28595, "ISO-8859-5")]
	public void Name_Writes_A_Code_Page_In_Its_Usual_Form(int codePage, string expected)
	{
		// Arrange
		FileEncoding sut = new()
		{
			Encoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage)!,
			HasByteOrderMark = false
		};

		// Act
		string name = sut.Name;

		// Assert
		name
			.Should()
			.Be(expected);
	}
	#endregion
}
