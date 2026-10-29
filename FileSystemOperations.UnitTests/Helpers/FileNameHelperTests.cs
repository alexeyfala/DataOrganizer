using FileSystemOperations.Helpers;

namespace FileSystemOperations.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(FileNameHelper)}"" type")]
internal class FileNameHelperTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileNameHelper.GetExtension" />: the extension comes from the name without the tail that Windows drops,
	/// and no name has none.
	/// </summary>
	[TestCase("notes.txt", ExpectedResult = ".txt")]
	[TestCase("notes.txt. .", ExpectedResult = ".txt")]
	[TestCase("notes", ExpectedResult = "")]
	[TestCase("", ExpectedResult = "")]
	[TestCase(null, ExpectedResult = "")]
	public string GetExtension_Reads_The_Extension_Without_The_Dropped_Tail(string? name)
	{
		return FileNameHelper
			.GetExtension(name)
			.ToString();
	}

	/// <summary>
	/// <see cref="FileNameHelper.TrimIgnoredTail" />: the dots and blanks that end a name are dropped, and the ones inside
	/// it are kept.
	/// </summary>
	[TestCase("notes. .", ExpectedResult = "notes")]
	[TestCase("a. b", ExpectedResult = "a. b")]
	[TestCase(" . ", ExpectedResult = "")]
	[TestCase("", ExpectedResult = "")]
	public string TrimIgnoredTail_Drops_The_Dots_And_Blanks_At_The_End(string fileName)
	{
		return FileNameHelper
			.TrimIgnoredTail(fileName)
			.ToString();
	}
	#endregion
}
