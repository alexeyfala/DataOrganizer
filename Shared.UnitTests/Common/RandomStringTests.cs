using AwesomeAssertions;
using Shared.Common;

namespace Shared.UnitTests.Common;

[TestFixture(Description = $@"Tests of ""{nameof(RandomString)}"" type")]
internal class RandomStringTests
{
	#region Methods
	/// <summary>
	/// <see cref="RandomString.Create" />: the string has the length asked for and holds capital letters and digits only.
	/// </summary>
	[Test]
	public void Create_Makes_A_String_Of_Capitals_And_Digits_Of_The_Length()
	{
		// Act
		string result = RandomString.Create(64);

		// Assert
		result
			.Should()
			.HaveLength(64)
			.And
			.MatchRegex("^[A-Z0-9]+$");
	}
	#endregion
}
