using AwesomeAssertions;
using DataOrganizer.Infrastructure.DialogContracts.Helpers;

namespace DataOrganizer.Infrastructure.UnitTests.DialogContracts.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(ObjectHeaderBuilder)}"" type")]
internal class ObjectHeaderBuilderTests
{
	#region Methods
	/// <summary>
	/// <see cref="ObjectHeaderBuilder.Build" />: the name of the object follows the action, in quotes.
	/// </summary>
	[Test]
	public void Build_Puts_The_Name_In_Quotes_After_The_Action()
	{
		// Act
		string result = ObjectHeaderBuilder.Build("Action", "Name");

		// Assert
		result
			.Should()
			.Be(@"Action ""Name""");
	}
	#endregion
}
