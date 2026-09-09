using DataOrganizer.Services.Explorer;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(LinuxExplorerManager)}"" type")]
internal class LinuxExplorerManagerTests
{
	#region Methods
	/// <summary>
	/// <see cref="LinuxExplorerManager.TitleMatchesFolderName" />: matches an exact title or a "&lt;name&gt; &lt;suffix&gt;" title, and rejects anything else.
	/// </summary>
	[TestCase("Docs", "Docs", ExpectedResult = true)]
	[TestCase("Docs - Files", "Docs", ExpectedResult = true)]
	[TestCase("Docs ", "Docs", ExpectedResult = true)]
	[TestCase("Documents", "Doc", ExpectedResult = false)]
	[TestCase("Other", "Docs", ExpectedResult = false)]
	[TestCase("", "Docs", ExpectedResult = false)]
	[TestCase("Docs", "", ExpectedResult = false)]
	public bool TitleMatchesFolderName_Matches_Exact_And_Prefixed_Titles(string title, string folderName)
	{
		return LinuxExplorerManager.TitleMatchesFolderName(title, folderName);
	}
	#endregion
}
