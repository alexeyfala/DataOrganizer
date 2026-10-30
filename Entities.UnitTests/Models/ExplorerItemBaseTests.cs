using AwesomeAssertions;
using Entities.Models;

namespace Entities.UnitTests.Models;

[TestFixture(Description = $@"Tests of ""{nameof(ExplorerItemBase)}"" type")]
internal class ExplorerItemBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExplorerItemBase.Name" />: an object made without a name has an empty one rather than none.
	/// </summary>
	[Test]
	public void Name_Is_Empty_For_An_Object_Made_Without_One()
	{
		// Act
		FileEntity result = new();

		// Assert
		result.Name
			.Should()
			.BeEmpty();
	}
	#endregion
}
