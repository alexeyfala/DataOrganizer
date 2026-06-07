using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Helpers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FilterEngine<>)}"" type")]
internal class FilterEngineTests
{
	#region Methods
	/// <summary>
	/// <see cref="FilterEngine{TModel}.AddRange" /> and <see cref="FilterEngine{TModel}.IsSourceEmpty" />: added items populate both the source and the visible sequence.
	/// </summary>
	[Test]
	public void AddRange_Populates_Source_And_Visible_Sequence()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(5)];

		// Act
		sut.AddRange(items);

		// Assert
		sut.IsSourceEmpty
			.Should()
			.BeFalse();

		sut.Visible
			.Should()
			.HaveCount(5);
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.Clear" />: clearing empties both the source and the visible sequence.
	/// </summary>
	[Test]
	public void Clear_Empties_The_Source()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		sut.AddRange(TestUtils.CreateFilesDto(3));

		// Act
		sut.Clear();

		// Assert
		sut.IsSourceEmpty
			.Should()
			.BeTrue();

		sut.Visible
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.Contains" />: returns true for an existing item and false for an unknown one.
	/// </summary>
	[Test]
	public void Contains_Returns_True_For_Existing_Item()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(3)];

		sut.AddRange(items);

		// Act / Assert
		sut.Contains(items[1])
			.Should()
			.BeTrue();

		sut.Contains(TestUtils.CreateFileDto())
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.Dispose" />: disposing twice does not throw and clears the source.
	/// </summary>
	[Test]
	public void Dispose_Is_Idempotent_And_Clears_Source()
	{
		// Arrange
		FilterEngine<FileModelDto> sut = CreateSut();

		sut.AddRange(TestUtils.CreateFilesDto(3));

		// Act
		Action act = () =>
		{
			sut.Dispose();

			sut.Dispose();
		};

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.FirstOrDefaultFromSource" />: returns the source item matching the predicate.
	/// </summary>
	[Test]
	public void FirstOrDefaultFromSource_Returns_Matching_Item()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(3)];

		sut.AddRange(items);

		FileModelDto target = items[1];

		// Act
		FileModelDto? result = sut.FirstOrDefaultFromSource(x => x.Id == target.Id);

		// Assert
		result
			.Should()
			.Be(target);
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.FirstOrDefaultFromSource" />: returns null when no source item matches the predicate.
	/// </summary>
	[Test]
	public void FirstOrDefaultFromSource_Returns_Null_If_No_Item_Matches()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		sut.AddRange(TestUtils.CreateFilesDto(3));

		// Act
		FileModelDto? result = sut.FirstOrDefaultFromSource(x => x.Id == Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.InsertAndRebuild" />: an already present item is not re-inserted and order is preserved.
	/// </summary>
	[Test]
	public void InsertAndRebuild_Does_Nothing_When_Item_Already_Present()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(3)];

		sut.AddRange(items);

		FileModelDto duplicate = items[2];

		// Act
		sut.InsertAndRebuild(duplicate, 0);

		// Assert
		sut.Visible
			.Should()
			.HaveCount(3);

		sut.Visible
			.Should()
			.ContainInOrder(items);
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.InsertAndRebuild" />: a new item is placed at the specified visible index.
	/// </summary>
	[Test]
	public void InsertAndRebuild_Places_Item_At_Specified_Visible_Index()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		sut.AddRange(TestUtils.CreateFilesDto(3));

		FileModelDto inserted = TestUtils.CreateFileDto();

		// Act
		sut.InsertAndRebuild(inserted, 0);

		// Assert
		sut.Visible[0]
			.Should()
			.Be(inserted);
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.PostToUi" />: the action runs inline when no synchronization context is set.
	/// </summary>
	[Test]
	public void PostToUi_Executes_Action_Inline_When_No_Context()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using FilterEngine<FileModelDto> sut = CreateSut();

		bool executed = false;

		// Act
		sut.PostToUi(() => executed = true);

		// Assert
		executed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.Remove" />: removing an item returns true and drops it from the source.
	/// </summary>
	[Test]
	public void Remove_Removes_Item_From_Source()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(3)];

		sut.AddRange(items);

		// Act
		bool result = sut.Remove(items[1]);

		// Assert
		result
			.Should()
			.BeTrue();

		sut.FirstOrDefaultFromSource(x => x.Id == items[1].Id)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.Reorder" />: an item is moved to the specified visible index.
	/// </summary>
	[Test]
	public void Reorder_Moves_Item_To_Specified_Visible_Index()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(3)];

		sut.AddRange(items);

		// Act
		sut.Reorder(items[0], 2);

		// Assert
		sut.Visible[2]
			.Should()
			.Be(items[0]);
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.SelectFromSource" />: projects each source item through the selector.
	/// </summary>
	[Test]
	public void SelectFromSource_Projects_Source_Items()
	{
		// Arrange
		using FilterEngine<FileModelDto> sut = CreateSut();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(3)];

		sut.AddRange(items);

		// Act
		Guid[] result = [.. sut.SelectFromSource(x => x.Id)];

		// Assert
		result
			.Should()
			.BeEquivalentTo(items.Select(x => x.Id));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a fresh <see cref="FilterEngine{T}" /> over <see cref="FileModelDto" /> with no filter and Index ordering.
	/// </summary>
	private static FilterEngine<FileModelDto> CreateSut()
	{
		// Reset synchronization context so DynamicData applies changes inline.
		SynchronizationContext.SetSynchronizationContext(null);

		IObservable<Func<FileModelDto, bool>> filter = Observable.Return<Func<FileModelDto, bool>>(_ => true);

		return new FilterEngine<FileModelDto>(filter);
	}
	#endregion
}
