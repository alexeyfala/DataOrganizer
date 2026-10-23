using AwesomeAssertions;
using DataOrganizer.Enums.Dataset;
using DataOrganizer.Helpers.Dataset;
using DataOrganizer.Models.Dataset;
using Shared.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Dataset;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetRecordTree)}"" type")]
internal class DatasetRecordTreeTests
{
	#region Methods
	/// <summary>
	/// <see cref="DatasetRecordTree.Add" />: a record added to a collapsed group opens the group.
	/// </summary>
	[Test]
	public void Add_Expands_The_Group()
	{
		// Arrange
		RecordsGroup group = new()
		{
			IsExpanded = false
		};

		ObservableCollection<DatasetRecordBase> records = [group];

		// Act
		DatasetRecordTree.Add(records, new ValueRecord(), group);

		// Assert
		group.IsExpanded
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Add" />: a new record goes after the records already there, in a group or at the root.
	/// </summary>
	[Test]
	public void Add_Puts_The_Record_Last([Values] bool toGroup)
	{
		// Arrange
		ValueRecord added = new();

		RecordsGroup group = new()
		{
			Children = [new ValueRecord(), new KeyValueRecord()]
		};

		ObservableCollection<DatasetRecordBase> records = [new ValueRecord(), group];

		// Whichever collection gets the record, the rest of the test reads the same.
		ObservableCollection<DatasetRecordBase> target = toGroup
			? group.Children
			: records;

		// Act
		DatasetRecordTree.Add(records, added, toGroup ? group : null);

		// Assert
		target
			.Should()
			.HaveCount(3);

		target[^1]
			.Should()
			.BeSameAs(added);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Delete" />: a record deep in the groups leaves its own group, and the other records
	/// stay.
	/// </summary>
	[Test]
	public void Delete_Removes_A_Record_Of_A_Nested_Group()
	{
		// Arrange
		ValueRecord kept = new();

		ValueRecord deleted = new();

		RecordsGroup inner = new()
		{
			Children = [kept, deleted]
		};

		RecordsGroup outer = new()
		{
			Children = [inner]
		};

		ValueRecord sibling = new();

		ObservableCollection<DatasetRecordBase> records = [outer, sibling];

		// Act
		DatasetRecordTree.Delete(records, deleted);

		// Assert
		inner.Children
			.Should()
			.Equal(kept);

		outer.Children
			.Should()
			.Equal(inner);

		records
			.Should()
			.Equal(outer, sibling);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Delete" />: a record at the root leaves the root, and the groups keep their records.
	/// </summary>
	[Test]
	public void Delete_Removes_A_Top_Level_Record()
	{
		// Arrange
		ValueRecord deleted = new();

		ValueRecord child = new();

		RecordsGroup group = new()
		{
			Children = [child]
		};

		ObservableCollection<DatasetRecordBase> records = [deleted, group];

		// Act
		DatasetRecordTree.Delete(records, deleted);

		// Assert
		records
			.Should()
			.Equal(group);

		group.Children
			.Should()
			.Equal(child);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.FindOwner" />: returns the owning children collection for a deeply nested record.
	/// </summary>
	[Test]
	public void FindOwner_Returns_Children_For_Nested_Record()
	{
		// Arrange
		ValueRecord deep = new() { Value = "deep" };

		RecordsGroup inner = new() { Name = "inner" };

		inner.Children.Add(deep);

		RecordsGroup outer = new() { Name = "outer" };

		outer.Children.Add(inner);

		ObservableCollection<DatasetRecordBase> root = [outer];

		// Act
		ObservableCollection<DatasetRecordBase>? owner = DatasetRecordTree.FindOwner(root, deep);

		// Assert
		owner
			.Should()
			.BeSameAs(inner.Children);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.FindOwner" />: returns <c>null</c> when the record is not present anywhere.
	/// </summary>
	[Test]
	public void FindOwner_Returns_Null_When_Absent()
	{
		// Arrange
		ObservableCollection<DatasetRecordBase> root = [new ValueRecord { Value = "A" }];

		ValueRecord stranger = new() { Value = "stranger" };

		// Act
		ObservableCollection<DatasetRecordBase>? owner = DatasetRecordTree.FindOwner(root, stranger);

		// Assert
		owner
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.FindOwner" />: returns the root collection for a top-level record.
	/// </summary>
	[Test]
	public void FindOwner_Returns_Root_For_Top_Level_Record()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ObservableCollection<DatasetRecordBase> root = [a];

		// Act
		ObservableCollection<DatasetRecordBase>? owner = DatasetRecordTree.FindOwner(root, a);

		// Assert
		owner
			.Should()
			.BeSameAs(root);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Flatten" />: lists the records in the order of the walk, the last record first, each
	/// group before its records and the last of them first.
	/// </summary>
	[Test]
	public void Flatten_Lists_The_Records_In_The_Order_Of_The_Walk()
	{
		// Arrange
		ValueRecord first = new() { Value = RandomString.Create(5) };

		ValueRecord nested = new() { Value = RandomString.Create(5) };

		ValueRecord last = new() { Value = RandomString.Create(5) };

		ValueRecord inGroup = new() { Value = RandomString.Create(5) };

		RecordsGroup group = new() { Name = RandomString.Create(5) };

		group.Children.Add(inGroup);

		group.Children.Add(nested);

		DatasetRecordBase[] records = [first, group, last];

		// Act
		DatasetRecordBase[] result = [.. DatasetRecordTree.Flatten(records)];

		// Assert
		result
			.Should()
			.Equal(last, group, nested, inGroup, first);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Flatten" />: returns every record including nested group children.
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
		DatasetRecordBase[] result = [.. DatasetRecordTree.Flatten(hierarchy)];

		// Assert
		result
			.Should()
			.HaveCount(4);

		result
			.Should()
			.Contain([root, nested, leaf1, leaf2]);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Move" />: a target index beyond the count is clamped to the last slot.
	/// </summary>
	[Test]
	public void Move_Clamps_Target_Index_Beyond_Count()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ValueRecord b = new() { Value = "B" };

		ObservableCollection<DatasetRecordBase> root = [a, b];

		// Act
		bool result = DatasetRecordTree.Move(root, a, root, 99);

		// Assert
		result
			.Should()
			.BeTrue();

		root
			.Should()
			.Equal(b, a);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Move" />: a target index beyond the count of another collection puts the record at its
	/// end.
	/// </summary>
	[Test]
	public void Move_Into_Another_Collection_Clamps_Target_Index()
	{
		// Arrange
		ValueRecord moved = new();

		ValueRecord child = new();

		RecordsGroup group = new()
		{
			Children = [child]
		};

		ObservableCollection<DatasetRecordBase> root = [moved, group];

		// Act
		bool result = DatasetRecordTree.Move(root, moved, group.Children, 99);

		// Assert
		result
			.Should()
			.BeTrue();

		group.Children
			.Should()
			.Equal(child, moved);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Move" />: moves a root record into a group, appended at the end.
	/// </summary>
	[Test]
	public void Move_Into_Group_Appends_At_End()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ValueRecord x = new() { Value = "X" };

		RecordsGroup group = new() { Name = "group" };

		group.Children.Add(x);

		ObservableCollection<DatasetRecordBase> root = [a, group];

		// Act
		bool result = DatasetRecordTree.Move(root, a, group.Children, group.Children.Count);

		// Assert
		result
			.Should()
			.BeTrue();

		root
			.Should()
			.Equal(group);

		group.Children
			.Should()
			.Equal(x, a);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Move" />: moves a record out of a group back to the root.
	/// </summary>
	[Test]
	public void Move_Out_Of_Group_To_Root()
	{
		// Arrange
		ValueRecord x = new() { Value = "X" };

		ValueRecord y = new() { Value = "Y" };

		RecordsGroup group = new() { Name = "group" };

		group.Children.Add(x);

		group.Children.Add(y);

		ObservableCollection<DatasetRecordBase> root = [group];

		// Act
		bool result = DatasetRecordTree.Move(root, x, root, root.Count);

		// Assert
		result
			.Should()
			.BeTrue();

		group.Children
			.Should()
			.Equal(y);

		root
			.Should()
			.Equal(group, x);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Move" />: returns <c>false</c> and changes nothing when the record is absent.
	/// </summary>
	[Test]
	public void Move_Returns_False_When_Record_Absent()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ValueRecord stranger = new() { Value = "stranger" };

		ObservableCollection<DatasetRecordBase> root = [a];

		// Act
		bool result = DatasetRecordTree.Move(root, stranger, root, 0);

		// Assert
		result
			.Should()
			.BeFalse();

		root
			.Should()
			.Equal(a);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Move" />: dropping a record onto its own slot leaves the order unchanged, raises no
	/// collection notification (the guard behind the anti-scroll-jump fix) and reports no move.
	/// </summary>
	[Test]
	public void Move_Within_Collection_NoOp_Raises_No_Notification()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ValueRecord b = new() { Value = "B" };

		ValueRecord c = new() { Value = "C" };

		ObservableCollection<DatasetRecordBase> root = [a, b, c];

		bool raised = false;

		root.CollectionChanged += (_, _) => raised = true;

		// Act: slot 2 for "B" (index 1) resolves to its own position.
		bool result = DatasetRecordTree.Move(root, b, root, 2);

		// Assert
		result
			.Should()
			.BeFalse();

		root
			.Should()
			.Equal(a, b, c);

		raised
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Move" />: within one collection, moves the record toward the end.
	/// </summary>
	[Test]
	public void Move_Within_Collection_Reorders_Down()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ValueRecord b = new() { Value = "B" };

		ValueRecord c = new() { Value = "C" };

		ObservableCollection<DatasetRecordBase> root = [a, b, c];

		// Act
		bool result = DatasetRecordTree.Move(root, a, root, 3);

		// Assert
		result
			.Should()
			.BeTrue();

		root
			.Should()
			.Equal(b, c, a);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Move" />: within one collection, moves the record toward the start.
	/// </summary>
	[Test]
	public void Move_Within_Collection_Reorders_Up()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ValueRecord b = new() { Value = "B" };

		ValueRecord c = new() { Value = "C" };

		ObservableCollection<DatasetRecordBase> root = [a, b, c];

		// Act
		bool result = DatasetRecordTree.Move(root, c, root, 0);

		// Assert
		result
			.Should()
			.BeTrue();

		root
			.Should()
			.Equal(c, a, b);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Sort" />: records of a kind the sort does not know would be lost, so the records keep
	/// their order.
	/// </summary>
	[Test]
	public void Sort_Keeps_Records_It_Cannot_Sort()
	{
		// Arrange
		ForeignRecord foreign = new();

		ValueRecord value = new()
		{
			Value = RandomString.Create(10)
		};

		ObservableCollection<DatasetRecordBase> records = [foreign, value];

		// Act
		bool result = DatasetRecordTree.Sort(records, ListSortDirection.Ascending);

		// Assert
		result
			.Should()
			.BeFalse();

		records
			.Should()
			.Equal(foreign, value);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Sort" />: groups come first, then key and value records, then value records, whatever
	/// the direction.
	/// </summary>
	[Test]
	public void Sort_Puts_Groups_First_And_Values_Last([Values] ListSortDirection direction)
	{
		// Arrange
		RecordsGroup group = new()
		{
			Name = RandomString.Create(10)
		};

		KeyValueRecord keyValue = new()
		{
			Key = RandomString.Create(10)
		};

		ValueRecord value = new()
		{
			Value = RandomString.Create(10)
		};

		ObservableCollection<DatasetRecordBase> records = [value, keyValue, group];

		// Act
		bool result = DatasetRecordTree.Sort(records, direction);

		// Assert
		result
			.Should()
			.BeTrue();

		records
			.Should()
			.Equal(group, keyValue, value);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Sort" />: the records of the groups count too, so a sort that would lose one deep in a
	/// group is refused.
	/// </summary>
	[Test]
	public void Sort_Refuses_A_Sort_That_Would_Lose_A_Nested_Record()
	{
		// Arrange
		RecordsGroup group = new()
		{
			Children = [new ForeignRecord(), new ValueRecord()]
		};

		ObservableCollection<DatasetRecordBase> records = [group];

		// Act
		bool result = DatasetRecordTree.Sort(records, ListSortDirection.Ascending);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.Sort" />: sorts groups, key-value records, and values in the specified direction.
	/// </summary>
	[Test]
	public void Sort_Sorts_Groups_KeyValues_And_Values_In_Specified_Direction([Values] ListSortDirection direction)
	{
		// Arrange
		RecordsGroup groupA = new() { Name = "A" };

		RecordsGroup groupB = new() { Name = "B" };

		// The values go the other way than the keys, which the records follow.
		KeyValueRecord kvX = new() { Key = "X", Value = "2" };

		KeyValueRecord kvY = new() { Key = "Y", Value = "1" };

		ValueRecord vP = new() { Value = "P" };

		ValueRecord vQ = new() { Value = "Q" };

		ObservableCollection<DatasetRecordBase> records = [groupB, kvY, vQ, kvX, vP, groupA];

		// Act
		bool result = DatasetRecordTree.Sort(records, direction);

		// Assert
		result
			.Should()
			.BeTrue();

		RecordsGroup[] groups = [.. records.OfType<RecordsGroup>()];

		KeyValueRecord[] keyValueRecords = [.. records.OfType<KeyValueRecord>()];

		ValueRecord[] values = [.. records.OfType<ValueRecord>()];

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

	/// <summary>
	/// <see cref="DatasetRecordTree.Sort" />: the records of the groups are sorted too, at any depth.
	/// </summary>
	[Test]
	public void Sort_Sorts_The_Records_Of_Nested_Groups()
	{
		// Arrange
		ValueRecord first = new()
		{
			Value = "a"
		};

		ValueRecord second = new()
		{
			Value = "b"
		};

		RecordsGroup inner = new()
		{
			Children = [second, first]
		};

		RecordsGroup outer = new()
		{
			Children = [inner]
		};

		ObservableCollection<DatasetRecordBase> records = [outer];

		// Act
		DatasetRecordTree.Sort(records, ListSortDirection.Ascending);

		// Assert
		inner.Children
			.Should()
			.Equal(first, second);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: a null context (empty surface) appends to the root.
	/// </summary>
	[Test]
	public void TryResolveTarget_Empty_Surface_Appends_To_Root()
	{
		// Arrange
		ValueRecord dragged = new() { Value = "dragged" };

		ValueRecord a = new() { Value = "A" };

		ObservableCollection<DatasetRecordBase> root = [a];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(
			root,
			dragged,
			null,
			0.5,
			out ObservableCollection<DatasetRecordBase> target,
			out int index,
			out DropPlacement placement);

		// Assert
		result
			.Should()
			.BeTrue();

		target
			.Should()
			.BeSameAs(root);

		index
			.Should()
			.Be(root.Count);

		placement
			.Should()
			.Be(DropPlacement.After);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: a group dropped onto another group goes into it, at the end.
	/// </summary>
	[Test]
	public void TryResolveTarget_Group_Into_Another_Group_Targets_Its_Children()
	{
		// Arrange
		RecordsGroup dragged = new()
		{
			Name = "dragged"
		};

		RecordsGroup other = new()
		{
			Children = [new ValueRecord()],
			Name = "other"
		};

		ObservableCollection<DatasetRecordBase> root = [dragged, other];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(
			root,
			dragged,
			other,
			0.5,
			out ObservableCollection<DatasetRecordBase> target,
			out int index,
			out DropPlacement placement);

		// Assert
		result
			.Should()
			.BeTrue();

		target
			.Should()
			.BeSameAs(other.Children);

		index
			.Should()
			.Be(other.Children.Count);

		placement
			.Should()
			.Be(DropPlacement.Into);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: refuses to drop a group into one of its descendants.
	/// </summary>
	[Test]
	public void TryResolveTarget_Group_Into_Descendant_Returns_False()
	{
		// Arrange
		RecordsGroup descendant = new() { Name = "descendant" };

		RecordsGroup group = new() { Name = "group" };

		group.Children.Add(descendant);

		ObservableCollection<DatasetRecordBase> root = [group];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(root, group, descendant, 0.5, out _, out _, out _);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: refuses to drop a group into itself.
	/// </summary>
	[Test]
	public void TryResolveTarget_Group_Into_Itself_Returns_False()
	{
		// Arrange
		RecordsGroup group = new() { Name = "group" };

		ObservableCollection<DatasetRecordBase> root = [group];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(root, group, group, 0.5, out _, out _, out _);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: refuses to drop a group next to one of its own records.
	/// </summary>
	[Test]
	public void TryResolveTarget_Group_Onto_A_Record_Inside_It_Returns_False()
	{
		// Arrange
		ValueRecord child = new();

		RecordsGroup group = new()
		{
			Children = [child]
		};

		ObservableCollection<DatasetRecordBase> root = [group];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(root, group, child, 0.8, out _, out _, out _);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: a group context targets its children, appended at the end.
	/// </summary>
	[Test]
	public void TryResolveTarget_Group_Targets_Children_At_End()
	{
		// Arrange
		ValueRecord dragged = new() { Value = "dragged" };

		ValueRecord x = new() { Value = "X" };

		RecordsGroup group = new() { Name = "group" };

		group.Children.Add(x);

		ObservableCollection<DatasetRecordBase> root = [group];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(
			root,
			dragged,
			group,
			0.9,
			out ObservableCollection<DatasetRecordBase> target,
			out int index,
			out DropPlacement placement);

		// Assert
		result
			.Should()
			.BeTrue();

		target
			.Should()
			.BeSameAs(group.Children);

		index
			.Should()
			.Be(group.Children.Count);

		placement
			.Should()
			.Be(DropPlacement.Into);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: refuses to drop a record onto itself.
	/// </summary>
	[Test]
	public void TryResolveTarget_Onto_Itself_Returns_False()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ObservableCollection<DatasetRecordBase> root = [a];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(root, a, a, 0.5, out _, out _, out _);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: over a record's lower half inserts after it.
	/// </summary>
	[Test]
	public void TryResolveTarget_Record_Lower_Half_Inserts_After()
	{
		// Arrange
		ValueRecord dragged = new() { Value = "dragged" };

		ValueRecord a = new() { Value = "A" };

		ValueRecord b = new() { Value = "B" };

		ObservableCollection<DatasetRecordBase> root = [a, b];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(
			root,
			dragged,
			b,
			0.8,
			out ObservableCollection<DatasetRecordBase> target,
			out int index,
			out DropPlacement placement);

		// Assert
		result
			.Should()
			.BeTrue();

		target
			.Should()
			.BeSameAs(root);

		placement
			.Should()
			.Be(DropPlacement.After);

		index
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="DatasetRecordTree.TryResolveTarget" />: over a record's upper half inserts before it.
	/// </summary>
	[Test]
	public void TryResolveTarget_Record_Upper_Half_Inserts_Before()
	{
		// Arrange
		ValueRecord dragged = new() { Value = "dragged" };

		ValueRecord a = new() { Value = "A" };

		ValueRecord b = new() { Value = "B" };

		ObservableCollection<DatasetRecordBase> root = [a, b];

		// Act
		bool result = DatasetRecordTree.TryResolveTarget(
			root,
			dragged,
			b,
			0.2,
			out _,
			out int index,
			out DropPlacement placement);

		// Assert
		result
			.Should()
			.BeTrue();

		placement
			.Should()
			.Be(DropPlacement.Before);

		index
			.Should()
			.Be(1);
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Record of a kind that the sort does not know.
	/// </summary>
	private sealed class ForeignRecord : DatasetRecordBase;
	#endregion
}
