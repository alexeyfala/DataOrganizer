using AwesomeAssertions;
using DataOrganizer.DTO.Dataset;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using System.Collections.ObjectModel;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetRecordMoveHelper)}"" type")]
internal class DatasetRecordMoveHelperTests
{
	#region Methods
	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.FindOwner" />: returns the owning children collection for a deeply nested record.
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
		ObservableCollection<DatasetRecordBase>? owner = DatasetRecordMoveHelper.FindOwner(root, deep);

		// Assert
		owner
			.Should()
			.BeSameAs(inner.Children);
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.FindOwner" />: returns <c>null</c> when the record is not present anywhere.
	/// </summary>
	[Test]
	public void FindOwner_Returns_Null_When_Absent()
	{
		// Arrange
		ObservableCollection<DatasetRecordBase> root = [new ValueRecord { Value = "A" }];

		ValueRecord stranger = new() { Value = "stranger" };

		// Act
		ObservableCollection<DatasetRecordBase>? owner = DatasetRecordMoveHelper.FindOwner(root, stranger);

		// Assert
		owner
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.FindOwner" />: returns the root collection for a top-level record.
	/// </summary>
	[Test]
	public void FindOwner_Returns_Root_For_Top_Level_Record()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ObservableCollection<DatasetRecordBase> root = [a];

		// Act
		ObservableCollection<DatasetRecordBase>? owner = DatasetRecordMoveHelper.FindOwner(root, a);

		// Assert
		owner
			.Should()
			.BeSameAs(root);
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.IsSelfOrDescendant" />: <c>False</c> for an unrelated collection.
	/// </summary>
	[Test]
	public void IsSelfOrDescendant_False_For_Unrelated_Collection()
	{
		// Arrange
		RecordsGroup group = new() { Name = "group" };

		ObservableCollection<DatasetRecordBase> unrelated = [new ValueRecord { Value = "A" }];

		// Act
		bool result = DatasetRecordMoveHelper.IsSelfOrDescendant(group, unrelated);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.IsSelfOrDescendant" />: <c>True</c> for a descendant group's children collection.
	/// </summary>
	[Test]
	public void IsSelfOrDescendant_True_For_Descendant_Children()
	{
		// Arrange
		RecordsGroup child = new() { Name = "child" };

		RecordsGroup parent = new() { Name = "parent" };

		parent.Children.Add(child);

		// Act
		bool result = DatasetRecordMoveHelper.IsSelfOrDescendant(parent, child.Children);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.IsSelfOrDescendant" />: <c>True</c> for the group's own children collection.
	/// </summary>
	[Test]
	public void IsSelfOrDescendant_True_For_Own_Children()
	{
		// Arrange
		RecordsGroup group = new() { Name = "group" };

		// Act
		bool result = DatasetRecordMoveHelper.IsSelfOrDescendant(group, group.Children);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.Move" />: a target index beyond the count is clamped to the last slot.
	/// </summary>
	[Test]
	public void Move_Clamps_Target_Index_Beyond_Count()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ValueRecord b = new() { Value = "B" };

		ObservableCollection<DatasetRecordBase> root = [a, b];

		// Act
		bool result = DatasetRecordMoveHelper.Move(root, a, root, 99);

		// Assert
		result
			.Should()
			.BeTrue();

		root
			.Should()
			.Equal(b, a);
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.Move" />: moves a root record into a group, appended at the end.
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
		bool result = DatasetRecordMoveHelper.Move(root, a, group.Children, group.Children.Count);

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
	/// <see cref="DatasetRecordMoveHelper.Move" />: moves a record out of a group back to the root.
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
		bool result = DatasetRecordMoveHelper.Move(group.Children, x, root, root.Count);

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
	/// <see cref="DatasetRecordMoveHelper.Move" />: returns <c>false</c> and changes nothing when the record is absent.
	/// </summary>
	[Test]
	public void Move_Returns_False_When_Record_Absent()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ValueRecord stranger = new() { Value = "stranger" };

		ObservableCollection<DatasetRecordBase> root = [a];

		// Act
		bool result = DatasetRecordMoveHelper.Move(root, stranger, root, 0);

		// Assert
		result
			.Should()
			.BeFalse();

		root
			.Should()
			.Equal(a);
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.Move" />: dropping a record onto its own slot leaves the order
	/// unchanged and raises no collection notification (the guard behind the anti-scroll-jump fix).
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
		bool result = DatasetRecordMoveHelper.Move(root, b, root, 2);

		// Assert
		result
			.Should()
			.BeTrue();

		root
			.Should()
			.Equal(a, b, c);

		raised
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.Move" />: within one collection, moves the record toward the end.
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
		bool result = DatasetRecordMoveHelper.Move(root, a, root, 3);

		// Assert
		result
			.Should()
			.BeTrue();

		root
			.Should()
			.Equal(b, c, a);
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.Move" />: within one collection, moves the record toward the start.
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
		bool result = DatasetRecordMoveHelper.Move(root, c, root, 0);

		// Assert
		result
			.Should()
			.BeTrue();

		root
			.Should()
			.Equal(c, a, b);
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.TryResolveTarget" />: a null context (empty surface) appends to the root.
	/// </summary>
	[Test]
	public void TryResolveTarget_Empty_Surface_Appends_To_Root()
	{
		// Arrange
		ValueRecord dragged = new() { Value = "dragged" };

		ValueRecord a = new() { Value = "A" };

		ObservableCollection<DatasetRecordBase> root = [a];

		// Act
		bool result = DatasetRecordMoveHelper.TryResolveTarget(
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
	/// <see cref="DatasetRecordMoveHelper.TryResolveTarget" />: refuses to drop a group into one of its descendants.
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
		bool result = DatasetRecordMoveHelper.TryResolveTarget(root, group, descendant, 0.5, out _, out _, out _);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.TryResolveTarget" />: refuses to drop a group into itself.
	/// </summary>
	[Test]
	public void TryResolveTarget_Group_Into_Itself_Returns_False()
	{
		// Arrange
		RecordsGroup group = new() { Name = "group" };

		ObservableCollection<DatasetRecordBase> root = [group];

		// Act
		bool result = DatasetRecordMoveHelper.TryResolveTarget(root, group, group, 0.5, out _, out _, out _);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.TryResolveTarget" />: a group context targets its children, appended at the end.
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
		bool result = DatasetRecordMoveHelper.TryResolveTarget(
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
	/// <see cref="DatasetRecordMoveHelper.TryResolveTarget" />: refuses to drop a record onto itself.
	/// </summary>
	[Test]
	public void TryResolveTarget_Onto_Itself_Returns_False()
	{
		// Arrange
		ValueRecord a = new() { Value = "A" };

		ObservableCollection<DatasetRecordBase> root = [a];

		// Act
		bool result = DatasetRecordMoveHelper.TryResolveTarget(root, a, a, 0.5, out _, out _, out _);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetRecordMoveHelper.TryResolveTarget" />: over a record's lower half inserts after it.
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
		bool result = DatasetRecordMoveHelper.TryResolveTarget(
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
	/// <see cref="DatasetRecordMoveHelper.TryResolveTarget" />: over a record's upper half inserts before it.
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
		bool result = DatasetRecordMoveHelper.TryResolveTarget(
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
}
