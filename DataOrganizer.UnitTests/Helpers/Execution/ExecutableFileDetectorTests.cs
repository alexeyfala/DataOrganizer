using AwesomeAssertions;
using DataOrganizer.Helpers.Execution;

namespace DataOrganizer.UnitTests.Helpers.Execution;

[TestFixture(Description = $@"Tests of ""{nameof(ExecutableFileDetector)}"" type")]
internal class ExecutableFileDetectorTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExecutableFileDetector.IsExecutable" />: classifies a file as executable by its extension,
	/// case-insensitively, and treats files without an extension as non-executable.
	/// </summary>
	[TestCase("app.exe", true)]
	[TestCase("C:\\folder\\app.exe", true)]
	[TestCase("script.SH", true)]
	[TestCase("installer.msi", true)]
	[TestCase("bundle.app", true)]
	[TestCase("tool.py", true)]
	[TestCase("readme", false)]
	[TestCase("noext.", false)]
	[TestCase("document.txt", false)]
	[TestCase("image.png", false)]
	[TestCase("archive.zip", false)]
	[TestCase("", false)]
	public void IsExecutable_Classifies_File_By_Extension(string fileName, bool expected)
	{
		// Act
		bool result = ExecutableFileDetector.IsExecutable(fileName);

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
