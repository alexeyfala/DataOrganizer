using AwesomeAssertions;
using DataOrganizer.Helpers.Dataset;
using DataOrganizer.Models.Dataset;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Dataset;

[TestFixture(Description = $@"Tests of ""{nameof(SampleRecords)}"" type")]
internal class SampleRecordsTests
{
	#region Methods
	/// <summary>
	/// <see cref="SampleRecords.CreateRandomRecords" />: another source gives the same values.
	/// </summary>
	[Test]
	public void CreateRandomRecords_Repeats_The_Values_From_Run_To_Run()
	{
		// Arrange
		string?[] first = [.. new SampleRecords().CreateRandomRecords(eachTypes: 3).OfType<ValueRecord>().Select(x => x.Value)];

		SampleRecords sut = new();

		// Act
		DatasetRecordBase[] records = [.. sut.CreateRandomRecords(eachTypes: 3)];

		// Assert
		records.OfType<ValueRecord>().Select(x => x.Value)
			.Should()
			.Equal(first);
	}

	/// <summary>
	/// <see cref="SampleRecords.CreateValueRecords" />: a share of the values is hidden, and the rest are shown.
	/// </summary>
	[Test]
	public void CreateValueRecords_Hides_A_Share_Of_The_Values()
	{
		// Arrange
		SampleRecords sut = new();

		// Act
		ValueRecord[] records = [.. sut.CreateValueRecords(20)];

		// Assert
		records
			.Should()
			.Contain(x => x.IsHidden)
			.And
			.Contain(x => !x.IsHidden);
	}
	#endregion
}
