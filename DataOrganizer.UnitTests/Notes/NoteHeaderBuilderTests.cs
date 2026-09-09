using AwesomeAssertions;
using DataOrganizer.Helpers.Notes;
using Shared.Common;
using Shared.Properties;

namespace DataOrganizer.UnitTests.TestTypes.Notes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteHeaderBuilder)}"" type")]
internal class NoteHeaderBuilderTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteHeaderBuilder.Build" />: a name is appended to the label.
	/// </summary>
	[Test]
	public void Build_Returns_The_Label_With_A_Name()
	{
		// Arrange
		string name = RandomString.Create(10);

		// Act
		string header = NoteHeaderBuilder.Build(name);

		// Assert
		header
			.Should()
			.Be($"{Strings.Note}: {name}");
	}

	/// <summary>
	/// <see cref="NoteHeaderBuilder.Build" />: a blank name leaves the label alone.
	/// </summary>
	[Test]
	public void Build_Returns_The_Label_Without_A_Name([Values(null, "", "   ")] string? name)
	{
		// Act
		string header = NoteHeaderBuilder.Build(name);

		// Assert
		header
			.Should()
			.Be(Strings.Note);
	}
	#endregion
}
