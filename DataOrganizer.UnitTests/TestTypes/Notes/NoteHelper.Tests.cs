using AwesomeAssertions;
using DataOrganizer.Helpers.Notes;
using Shared.Common;
using Shared.Properties;

namespace DataOrganizer.UnitTests.TestTypes.Notes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteHelper)}"" type")]
internal class NoteHelperTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteHelper.BuildHeader" />: a name is appended to the label.
	/// </summary>
	[Test]
	public void BuildHeader_Returns_The_Label_With_A_Name()
	{
		// Arrange
		string name = AppUtils.CreateRandomString(10);

		// Act
		string header = NoteHelper.BuildHeader(name);

		// Assert
		header
			.Should()
			.Be($"{Strings.Note}: {name}");
	}

	/// <summary>
	/// <see cref="NoteHelper.BuildHeader" />: a blank name leaves the label alone.
	/// </summary>
	[Test]
	public void BuildHeader_Returns_The_Label_Without_A_Name([Values(null, "", "   ")] string? name)
	{
		// Act
		string header = NoteHelper.BuildHeader(name);

		// Assert
		header
			.Should()
			.Be(Strings.Note);
	}
	#endregion
}
