using AwesomeAssertions;
using DatasetRecords.Models;

namespace DatasetRecords.UnitTests.Models;

[TestFixture(Description = $@"Tests of ""{nameof(RecordsGroup)}"" type")]
internal class RecordsGroupTests
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
