using AwesomeAssertions;
using DataOrganizer.Documents.FileContents.Helpers;

namespace DataOrganizer.Documents.UnitTests.FileContents.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(FileContentsMessages)}"" type")]
internal class FileContentsMessagesTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileContentsMessages.Empty" />: the message ends with the name of the file in quotes.
	/// </summary>
	[Test]
	public void Empty_Ends_With_The_Name_In_Quotes()
	{
		// Act
		string result = FileContentsMessages.Empty("Name");

		// Assert
		result
			.Should()
			.EndWith(@" ""Name""");
	}

	/// <summary>
	/// <see cref="FileContentsMessages.FailedToLoad" />: the message ends with the name of the file in quotes.
	/// </summary>
	[Test]
	public void FailedToLoad_Ends_With_The_Name_In_Quotes()
	{
		// Act
		string result = FileContentsMessages.FailedToLoad("Name");

		// Assert
		result
			.Should()
			.EndWith(@" ""Name""");
	}

	/// <summary>
	/// <see cref="FileContentsMessages.NotText" />: the message ends with the name of the file in quotes.
	/// </summary>
	[Test]
	public void NotText_Ends_With_The_Name_In_Quotes()
	{
		// Act
		string result = FileContentsMessages.NotText("Name");

		// Assert
		result
			.Should()
			.EndWith(@" ""Name""");
	}
	#endregion
}
