using AwesomeAssertions;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Helpers.Documents;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Helpers.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(FileTextStatusRules)}"" type")]
internal class FileTextStatusRulesTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileTextStatusRules.HoldsText" />: only a text holds one, and only a status without a text that is not
	/// closed runs its own reaction, once.
	/// </summary>
	[TestCase(FileTextStatus.Text, true, new string[0])]
	[TestCase(FileTextStatus.Closed, false, new string[0])]
	[TestCase(FileTextStatus.Damaged, false, new[] { "damaged" })]
	[TestCase(FileTextStatus.NotText, false, new[] { "not text" })]
	[TestCase(FileTextStatus.Empty, false, new[] { "empty" })]
	public void HoldsText_Runs_The_Reaction_Of_The_Status(FileTextStatus status, bool expected, string[] reactions)
	{
		// Arrange
		List<string> run = [];

		// Act
		bool result = FileTextStatusRules.HoldsText(
			status,
			() => run.Add("damaged"),
			() => run.Add("not text"),
			() => run.Add("empty"));

		// Assert
		result
			.Should()
			.Be(expected);

		run
			.Should()
			.Equal(reactions);
	}
	#endregion
}
