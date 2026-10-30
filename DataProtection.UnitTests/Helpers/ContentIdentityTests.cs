using AwesomeAssertions;
using DataProtection.Enums;
using DataProtection.Helpers;
using System;
using System.Collections.Generic;

namespace DataProtection.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(ContentIdentity)}"" type")]
internal class ContentIdentityTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentIdentity.ToAssociatedData" />: each purpose is authenticated as its own,
	/// so a blob of one purpose cannot be opened as another.
	/// </summary>
	[Test]
	public void ToAssociatedData_Tells_Every_Purpose_Apart()
	{
		// Arrange
		List<string> written = [];

		// Act
		foreach (ContentPurpose purpose in Enum.GetValues<ContentPurpose>())
		{
			ContentIdentity identity = new(purpose);

			written.Add(Convert.ToHexString(identity.ToAssociatedData()));
		}

		// Assert
		written
			.Should()
			.OnlyHaveUniqueItems();
	}
	#endregion
}
