using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.ViewModels;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(CopyHistoryViewModel)}"" type")]
internal class CopyHistoryViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="CopyHistoryViewModel.Clear" />.
	/// </summary>
	[Test]
	public void Clear_Clears_History()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.AddTestCopyHistory(TestUtils.CreateFilesDto(10));

		sut.SelectedItem = TestUtils.CreateFileDto();

		sut.HistorySearch = "SomeValue";

		// Act
		sut.Clear();

		// Assert
		sut.IsCopyHistoryEmpty
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
	/// Test of <see cref="CopyHistoryViewModel.Dispose" />.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.AddTestCopyHistory(TestUtils.CreateFilesDto(10));

		sut.SelectedItem = TestUtils.CreateFileDto();

		// Act
		sut.Dispose();

		// Assert
		sut.IsCopyHistoryEmpty
			.Should()
			.BeTrue();

		sut.SelectedItem
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="CopyHistoryViewModel.GetIdentifiers" />.
	/// </summary>
	[Test]
	public void GetIdentifiers_Returns_Identifiers_Of_Objects_In_History()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		FileModelDto[] items = TestUtils.CreateFilesDto(10).ToArray();

		sut.AddTestCopyHistory(items);

		// Act
		Guid[] result = [.. sut.GetIdentifiers()];

		// Assert
		result.Should()
			.Contain(items.Select(x => x.Id));
	}

	/// <summary>
	/// Test of <see cref="CopyHistoryViewModel.Initialize" />.
	/// </summary>
	[Test]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(10)];

		// Act
		sut.Initialize(items, items[0].Id);

		// Assert
		sut.IsCopyHistoryEmpty
			.Should()
			.BeFalse();

		sut.SelectedItem
			.Should()
			.NotBeNull();
	}
	#endregion
}
