using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.ViewModels;
using System;
using System.Linq;
using System.Threading;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(CopyHistoryViewModel)}"" type")]
internal class CopyHistoryViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="CopyHistoryViewModel.Clear" />: clears items, search text and selection.
	/// </summary>
	[Test]
	public void Clear_Clears_History()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.AddTestCopyHistory(TestUtils.CreateFilesDto(5));

		sut.SelectedItem = TestUtils.CreateFileDto();

		sut.HistorySearch = "SomeValue";

		// Act
		sut.Clear();

		// Assert
		sut.IsEmpty
			.Should()
			.BeTrue();

		sut.HistorySearch
			.Should()
			.BeNull();

		sut.SelectedItem
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel" /> constructor.
	/// </summary>
	[Test]
	public void Constructor_Initializes_Empty_History()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Assert
		sut.IsEmpty
			.Should()
			.BeTrue();

		sut.Items
			.Should()
			.BeEmpty();

		sut.SelectedItem
			.Should()
			.BeNull();

		sut.HistorySearch
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Dispose" />: clears items and selection.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.AddTestCopyHistory(TestUtils.CreateFilesDto(5));

		sut.SelectedItem = TestUtils.CreateFileDto();

		// Act
		sut.Dispose();

		// Assert
		sut.IsEmpty
			.Should()
			.BeTrue();

		sut.SelectedItem
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.GetIdentifiers" />: returns the ids of all items in history.
	/// </summary>
	[Test]
	public void GetIdentifiers_Returns_Identifiers_Of_Objects_In_History()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(5)];

		sut.AddTestCopyHistory(items);

		// Act
		Guid[] result = [.. sut.GetIdentifiers()];

		// Assert
		result.Should()
			.Contain(items.Select(x => x.Id));
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Initialize" />: populates history and selects the given item.
	/// </summary>
	[Test]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(5)];

		// Act
		sut.Initialize(items, items[0].Id);

		// Assert
		sut.IsEmpty
			.Should()
			.BeFalse();

		sut.SelectedItem
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.InsertOrMoveToTop" />: inserts a new item into the history.
	/// </summary>
	[Test]
	public void InsertOrMoveToTop_Inserts_New_Item_At_Top()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		FileModelDto[] existing = [.. TestUtils.CreateFilesDto(3)];

		sut.AddTestCopyHistory(existing);

		FileModelDto newItem = TestUtils.CreateFileDto();

		// Act
		sut.InsertOrMoveToTop(newItem);

		// Assert
		sut.Items
			.Should()
			.Contain(newItem);
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.InsertOrMoveToTop" />: moves an existing item without duplicating it.
	/// </summary>
	[Test]
	public void InsertOrMoveToTop_Moves_Existing_Item_Without_Duplication()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		FileModelDto[] existing = [.. TestUtils.CreateFilesDto(3)];

		sut.AddTestCopyHistory(existing);

		int initialCount = sut.Items.Count;

		// Act
		sut.InsertOrMoveToTop(existing[2]);

		// Assert
		sut.Items.Count
			.Should()
			.Be(initialCount);

		sut.Items
			.Should()
			.Contain(existing[2]);
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Remove" />: returns false when the item is not in history.
	/// </summary>
	[Test]
	public void Remove_Returns_False_When_Item_Is_Not_Present()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		bool result = sut.Remove(TestUtils.CreateFileDto());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Remove" />: returns true and removes the item from history.
	/// </summary>
	[Test]
	public void Remove_Returns_True_And_Removes_Item_From_History()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		FileModelDto[] existing = [.. TestUtils.CreateFilesDto(3)];

		sut.AddTestCopyHistory(existing);

		// Act
		bool result = sut.Remove(existing[1]);

		// Assert
		result
			.Should()
			.BeTrue();

		sut.Items
			.Should()
			.NotContain(existing[1]);
	}
	#endregion
}
