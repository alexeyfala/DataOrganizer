using AwesomeAssertions;
using DataOrganizer.Documents.FileContents.Dto;
using DataOrganizer.Documents.FileContents.Enums;

namespace DataOrganizer.Documents.UnitTests.FileContents.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(FileTextReading)}"" type")]
internal class FileTextReadingTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileTextReading.Text" />: a reading made without a text has an empty one rather than none.
	/// </summary>
	[Test]
	public void Text_Is_Empty_For_A_Reading_Without_Text()
	{
		// Act
		FileTextReading result = new()
		{
			Status = FileTextStatus.NotText
		};

		// Assert
		result.Text
			.Should()
			.BeEmpty();
	}
	#endregion
}
