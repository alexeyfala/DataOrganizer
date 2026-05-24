using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Models;
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
	/// Test of <see cref="FilterEngine{TModel}.AddRange" /> and <see cref="FilterEngine{TModel}.IsSourceEmpty" />.
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
	/// Test of <see cref="FilterEngine{TModel}.Clear" />.
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
	/// Test of <see cref="FilterEngine{TModel}.Contains" />.
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
	/// Test of <see cref="FilterEngine{TModel}.Dispose" />.
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
	/// Test of <see cref="FilterEngine{TModel}.FirstOrDefaultFromSource" />.
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
	/// Test of <see cref="FilterEngine{TModel}.FirstOrDefaultFromSource" />.
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
	/// Test of <see cref="FilterEngine{TModel}.InsertAndRebuild" />.
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
	/// Test of <see cref="FilterEngine{TModel}.InsertAndRebuild" />.
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
	/// Test of <see cref="FilterEngine{TModel}.PostToUi" />.
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
	/// Test of <see cref="FilterEngine{TModel}.Remove" />.
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
	/// Test of <see cref="FilterEngine{TModel}.Reorder" />.
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
	/// Test of <see cref="FilterEngine{TModel}.SelectFromSource" />.
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
