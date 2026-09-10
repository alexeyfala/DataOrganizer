using AwesomeAssertions;
using Entities.Enums;
using System;
using System.Linq;

namespace Entities.UnitTests.Guards;

[TestFixture(Description = $@"Guards the numbers ""{nameof(EntityKind)}"" is stored with")]
internal class EntityKindNumberingTests
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
		((int)EntityKind.Folder)
			.Should()
			.Be(0);

		((int)EntityKind.File)
			.Should()
			.Be(1);

		((int)EntityKind.Dataset)
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
			.GetValues<EntityKind>()
			.Select(x => (int)x)
			.Order()];

		// Assert
		numbers
			.Should()
			.Equal(0, 1, 2);
	}
	#endregion
}
