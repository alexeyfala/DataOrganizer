using AwesomeAssertions;
using DataOrganizer.Documents.DatasetRecords.Models;

namespace DataOrganizer.Documents.UnitTests.DatasetRecords.Models;

/// <summary>
/// Tests of <see cref="RecordsGroup" /> type.
/// </summary>
[TestFixture]
internal sealed class RecordsGroupTests
{
	#region Methods
	/// <summary>
	/// <see cref="RecordsGroup.IsExpanded" />: a new group shows its records.
	/// </summary>
	[Test]
	public void IsExpanded_Is_True_For_A_New_Group()
	{
		// Act
		RecordsGroup result = new();

		// Assert
		result.IsExpanded
			.Should()
			.BeTrue();
	}
	#endregion
}
