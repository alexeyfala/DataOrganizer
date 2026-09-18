using AwesomeAssertions;
using DataOrganizer.Helpers.Notes;
using Shared.Common;

namespace DataOrganizer.UnitTests.Helpers.Notes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteHeaderBuilder)}"" type")]
internal class NoteHeaderBuilderTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteHeaderBuilder.Build" />: a name is appended to the label a blank name leaves alone.
	/// </summary>
	[Test]
	public void Build_Appends_The_Name_To_The_Label([Values(null, "", "   ")] string? blank)
	{
		// Arrange
		string name = RandomString.Create(10);

		// Act
		string label = NoteHeaderBuilder.Build(blank);

		string header = NoteHeaderBuilder.Build(name);

		// Assert
		header
			.Should()
			.Be($"{label}: {name}");
	}
	#endregion
}
