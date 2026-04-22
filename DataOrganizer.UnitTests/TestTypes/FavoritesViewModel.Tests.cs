using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using NSubstitute;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FavoritesViewModel)}"" type")]
internal class FavoritesViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="FavoritesViewModel.AddHierarchy" />.
	/// </summary>
	[Test]
	public void AddHierarchy_Adds_Objects_To_Hierarchy_Property()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		ExplorerModelBaseDto[] hierarchy = [.. TestUtils.CreateFoldersDto(5).Concat<ExplorerModelBaseDto>(TestUtils.CreateFilesDto(5))];

		// Act
		sut.AddHierarchy(hierarchy);

		// Assert
		sut.Hierarchy
			.Should()
			.Contain(hierarchy);
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.ClosePopupByEsc" />.
	/// </summary>
	[Test]
	public void ClosePopupByEsc_Closes_Popup()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.IsPopupFixed = true;

		sut.IsPopupOpen = true;

		// Act
		sut.ClosePopupByEsc();

		// Assert
		sut.IsPopupFixed
			.Should()
			.BeFalse();

		sut.IsPopupOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.Dispose" />.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.PopupContent = new();

		const int count = 5;

		sut
			.FavoritesSettings
			.Categories
			.AddRange(TestUtils.CreateFavoriteCategories(count));

		sut
			.FavoritesSettings
			.OrderedCategories
			.AddRange(TestUtils.CreateGuids(count));

		sut
			.FavoritesSettings
			.SelectedPairs
			.AddRange(TestUtils.CreateCategoryFavoritePairs(count));

		sut
			.CopyHistorySettings
			.Items
			.AddRange(TestUtils.CreateGuids(count));

		// Act
		sut.Dispose();

		// Assert
		sut.FavoritesSettings.Categories
			.Should()
			.BeEmpty();

		sut.FavoritesSettings.OrderedCategories
			.Should()
			.BeEmpty();

		sut.FavoritesSettings.SelectedPairs
			.Should()
			.BeEmpty();

		sut.CopyHistorySettings.Items
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.Initialize" />.
	/// </summary>
	[AvaloniaTest]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		int positiveValue = TestUtils.CreateRandomInt(100, 300);

		FavoritesWindowSettings windowSettings = new()
		{
			PopupHeight = positiveValue,
			PopupWidth = positiveValue,
			X = positiveValue,
			Y = positiveValue
		};

		FavoritesViewSettings favoritesSettings = new()
		{
			NavigationColumnWidth = positiveValue - 20,
			OrderedCategories = [.. TestUtils.CreateGuids(5)],
			SelectedCategoryId = Guid.NewGuid(),
			SelectedPairs = [.. TestUtils.CreateCategoryFavoritePairs(5)]
		};

		CopyHistoryViewSettings copyHistorySettings = new()
		{
			Items = [.. TestUtils.CreateGuids(5)],
			SelectedItemId = Guid.NewGuid()
		};

		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		Window window = new();

		// Act
		sut.Initialize(
			window,
			windowSettings,
			favoritesSettings,
			copyHistorySettings);

		// Assert
		window.Position.X
			.Should()
			.Be(windowSettings.X);

		window.Position.Y
			.Should()
			.Be(windowSettings.Y);

		sut.PopupWidth
			.Should()
			.Be(windowSettings.PopupWidth);

		sut.PopupHeight
			.Should()
			.Be(windowSettings.PopupHeight);

		sut.FavoritesSettings.NavigationColumnWidth
			.Should()
			.Be(favoritesSettings.NavigationColumnWidth);

		sut.FavoritesSettings.SelectedCategoryId
			.Should()
			.Be(favoritesSettings.SelectedCategoryId);

		sut.FavoritesSettings.SelectedPairs
			.Should()
			.Contain(favoritesSettings.SelectedPairs);

		sut.FavoritesSettings.OrderedCategories
			.Should()
			.Contain(favoritesSettings.OrderedCategories);

		sut.CopyHistorySettings.SelectedItemId
			.Should()
			.Be(copyHistorySettings.SelectedItemId);

		sut.CopyHistorySettings.Items
			.Should()
			.Contain(copyHistorySettings.Items);
	}

	/// <summary>
	/// Test of <see cref="ViewModelBase.InsertToCopyHistory" />.
	/// </summary>
	[Test]
	public void InsertToCopyHistory_Inserts_New_Value_To_Top()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.CopyHistorySettings
			.Items
			.AddRange(TestUtils.CreateGuids(5));

		// Act
		sut.InsertToCopyHistory(file, false);

		// Assert
		sut.CopyHistorySettings.Items[0]
			.Should()
			.Be(file.Id);
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.ShowInEditorAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowInEditorAsync_Shows_Editor_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			viewLauncher.ConfigureEditorWindow(
				Arg.Any<IEnumerable<ExplorerModelBaseDto>>(),
				Arg.Any<IEnumerable<FileModelDto>>(),
				Arg.Any<IEnumerable<FileModelDto>>())
			.Returns(mock.Create<EditorWindow>());

			builder.RegisterInstance(viewLauncher);
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.IsShutdown
			.Should()
			.BeTrue();

		// Act
		await sut.ShowInEditorAsync(null, default);

		// Assert
		sut.IsShutdown
			.Should()
			.BeFalse();

		viewLauncher.Received().ConfigureEditorWindow(
			Arg.Any<IEnumerable<ExplorerModelBaseDto>>(),
			Arg.Any<IEnumerable<FileModelDto>>(),
			Arg.Any<IEnumerable<FileModelDto>>());
	}
	#endregion
}
