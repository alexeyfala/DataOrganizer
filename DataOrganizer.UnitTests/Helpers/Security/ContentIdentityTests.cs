using AwesomeAssertions;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Helpers.Security;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Helpers.Security;

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
		Guid id = Guid.NewGuid();

		List<string> written = [];

		// Act
		foreach (ContentPurpose purpose in Enum.GetValues<ContentPurpose>())
		{
			ContentIdentity identity = new(id, purpose);

			written.Add(Convert.ToHexString(identity.ToAssociatedData()));
		}

		// Assert
		written
			.Should()
			.OnlyHaveUniqueItems();
	}
	#endregion
}
