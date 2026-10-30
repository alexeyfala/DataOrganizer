using AppLifecycle.Services;
using AwesomeAssertions;
using Hierarchy.UnitTests.Factories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AppLifecycle.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(WorkSession)}"" type")]
internal class WorkSessionTests
{
	#region Methods
	/// <summary>
	/// <see cref="WorkSession.Clear" />: no object stays expanded or selected.
	/// </summary>
	[Test]
	public void Clear_Forgets_Every_Expanded_And_Selected_Object()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		WorkSession sut = new();

		sut.SetExpanded(id, true);

		sut.SetSelected(id, true);

		// Act
		sut.Clear();

		// Assert
		bool[] states = [sut.IsExpanded(id), sut.IsSelected(id)];

		states
			.Should()
			.OnlyContain(static x => !x);
	}

	/// <summary>
	/// <see cref="WorkSession.Remember" />: the objects stored expanded and selected are marked, the others stay as they
	/// were.
	/// </summary>
	[Test]
	public void Remember_Marks_The_Objects_Stored_Expanded_And_Selected()
	{
		// Arrange
		Guid expanded = Guid.NewGuid();

		Guid selected = Guid.NewGuid();

		Guid kept = Guid.NewGuid();

		WorkSession sut = new();

		sut.SetExpanded(kept, true);

		// Act
		sut.Remember(ItemDtoFactory.CreateLoadedHierarchy(
			[],
			expandedIds: [expanded],
			selectedIds: [selected]));

		// Assert
		bool[] states =
		[
			sut.IsExpanded(expanded),
			sut.IsSelected(selected),
			sut.IsExpanded(kept),
			sut.IsExpanded(selected),
			sut.IsSelected(expanded)
		];

		states
			.Should()
			.Equal(true, true, true, false, false);
	}

	/// <summary>
	/// <see cref="WorkSession.SetExpanded" />: an object is marked expanded and then collapsed again.
	/// </summary>
	[Test]
	public void SetExpanded_Marks_And_Clears_An_Object()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		WorkSession sut = new();

		// Act
		sut.SetExpanded(id, true);

		bool isExpanded = sut.IsExpanded(id);

		sut.SetExpanded(id, false);

		// Assert
		isExpanded
			.Should()
			.BeTrue();

		sut.IsExpanded(id)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="WorkSession.SetExpanded" />: folders expanded at the same time from many threads all stay expanded.
	/// </summary>
	[Test]
	public async Task SetExpanded_Keeps_Every_Object_Marked_In_Parallel()
	{
		// Arrange
		Guid[] ids = [.. Enumerable.Range(0, 10_000).Select(static _ => Guid.NewGuid())];

		WorkSession sut = new();

		// Act
		await Parallel.ForEachAsync(ids, (id, _) =>
		{
			sut.SetExpanded(id, true);

			return ValueTask.CompletedTask;
		});

		// Assert
		ids.Select(sut.IsExpanded)
			.Should()
			.OnlyContain(static x => x);
	}

	/// <summary>
	/// <see cref="WorkSession.SetSelected" />: an object is marked selected and then cleared again.
	/// </summary>
	[Test]
	public void SetSelected_Marks_And_Clears_An_Object()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		WorkSession sut = new();

		// Act
		sut.SetSelected(id, true);

		bool isSelected = sut.IsSelected(id);

		sut.SetSelected(id, false);

		// Assert
		isSelected
			.Should()
			.BeTrue();

		sut.IsSelected(id)
			.Should()
			.BeFalse();
	}
	#endregion
}
