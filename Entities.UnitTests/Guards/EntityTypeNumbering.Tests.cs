using AwesomeAssertions;
using Entities.Enums;
using System;
using System.Linq;

namespace Entities.UnitTests.Guards;

[TestFixture(Description = $@"Guards the numbers ""{nameof(EntityType)}"" is stored with")]
internal class EntityTypeNumberingTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityType" />: the database holds the numbers, and reordering the members would
	/// give every row written so far another meaning.
	/// </summary>
	[Test]
	public void Members_Keep_Their_Numbers()
	{
		// Assert
		((int)EntityType.Folder)
			.Should()
			.Be(0);

		((int)EntityType.File)
			.Should()
			.Be(1);

		((int)EntityType.DataSet)
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="EntityType" />: a member may only be appended, which shows up here as one more number.
	/// </summary>
	[Test]
	public void Nothing_Is_Numbered_Beyond_The_Known_Members()
	{
		// Act
		int[] numbers = [.. Enum
			.GetValues<EntityType>()
			.Select(x => (int)x)
			.Order()];

		// Assert
		numbers
			.Should()
			.Equal(0, 1, 2);
	}
	#endregion
}
