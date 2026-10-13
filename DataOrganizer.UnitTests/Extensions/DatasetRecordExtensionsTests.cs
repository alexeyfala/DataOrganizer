using AwesomeAssertions;
using DataOrganizer.Extensions;
using DataOrganizer.Models.Dataset;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetRecordExtensions)}"" type")]
internal class DatasetRecordExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="DatasetRecordExtensions.Flatten" />: returns every record including nested group children.
	/// </summary>
	[Test]
	public void Flatten_Returns_Every_Record_Including_Nested_Group_Children()
	{
		// Arrange
		ValueRecord leaf1 = new() { Value = RandomString.Create(5) };

		ValueRecord leaf2 = new() { Value = RandomString.Create(5) };

		RecordsGroup nested = new() { Name = RandomString.Create(5) };

		nested.Children.Add(leaf2);

		RecordsGroup root = new() { Name = RandomString.Create(5) };

		root.Children.Add(leaf1);

		root.Children.Add(nested);

		DatasetRecordBase[] hierarchy = [root];

		// Act
		DatasetRecordBase[] result = [.. hierarchy.Flatten()];

		// Assert
		result
			.Should()
			.HaveCount(4);

		result
			.Should()
			.Contain([root, nested, leaf1, leaf2]);
	}

	/// <summary>
	/// <see cref="DatasetRecordExtensions.GetCount(IEnumerable{DatasetRecordBase})" />: returns the total record count including children.
	/// </summary>
	[Test]
	public void GetCount_For_DatasetRecord_Returns_Total_Count_Including_Children()
	{
		// Arrange
		RecordsGroup group = new() { Name = RandomString.Create(5) };

		group.Children.Add(new ValueRecord { Value = RandomString.Create(5) });

		group.Children.Add(new ValueRecord { Value = RandomString.Create(5) });

		DatasetRecordBase[] hierarchy = [group];

		// Act
		int result = hierarchy.GetCount();

		// Assert
		result
			.Should()
			.Be(3);
	}

	/// <summary>
	/// <see cref="DatasetRecordExtensions.Sort" />: sorts groups, key-value records, and values in the specified direction.
	/// </summary>
	[Test]
	public void Sort_Sorts_Groups_KeyValues_And_Values_In_Specified_Direction([Values] ListSortDirection direction)
	{
		// Arrange
		RecordsGroup groupA = new() { Name = "A" };

		RecordsGroup groupB = new() { Name = "B" };

		KeyValueRecord kvX = new() { Key = "X", Value = "1" };

		KeyValueRecord kvY = new() { Key = "Y", Value = "2" };

		ValueRecord vP = new() { Value = "P" };

		ValueRecord vQ = new() { Value = "Q" };

		DatasetRecordBase[] records = [groupB, kvY, vQ, kvX, vP, groupA];

		// Act
		DatasetRecordBase[] result = records.Sort(direction);

		// Assert
		RecordsGroup[] groups = [.. result.OfType<RecordsGroup>()];

		KeyValueRecord[] keyValueRecords = [.. result.OfType<KeyValueRecord>()];

		ValueRecord[] values = [.. result.OfType<ValueRecord>()];

		if (direction == ListSortDirection.Ascending)
		{
			groups
				.Select(x => x.Name)
				.Should()
				.Equal("A", "B");

			keyValueRecords
				.Select(x => x.Key)
				.Should()
				.Equal("X", "Y");

			values
				.Select(x => x.Value)
				.Should()
				.Equal("P", "Q");
		}
		else
		{
			groups
				.Select(x => x.Name)
				.Should()
				.Equal("B", "A");

			keyValueRecords
				.Select(x => x.Key)
				.Should()
				.Equal("Y", "X");

			values
				.Select(x => x.Value)
				.Should()
				.Equal("Q", "P");
		}
	}
	#endregion
}
