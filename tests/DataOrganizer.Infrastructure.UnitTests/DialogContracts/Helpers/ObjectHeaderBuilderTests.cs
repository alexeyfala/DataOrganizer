using AwesomeAssertions;
using DataOrganizer.Infrastructure.DialogContracts.Helpers;
using DataOrganizer.Shared.Helpers;

namespace DataOrganizer.Infrastructure.UnitTests.DialogContracts.Helpers;

/// <summary>
/// Tests of <see cref="ObjectHeaderBuilder" /> type.
/// </summary>
[TestFixture]
internal sealed class ObjectHeaderBuilderTests
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
			.Be($"Action {QuoteBuilder.Build("Name")}");
	}
	#endregion
}
