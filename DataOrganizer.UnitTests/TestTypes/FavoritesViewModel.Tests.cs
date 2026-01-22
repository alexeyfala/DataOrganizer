using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using NSubstitute;
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

		ExplorerModelBaseDto[] hierarchy = [.. TestUtils.CreateFoldersDto(10).Concat<ExplorerModelBaseDto>(TestUtils.CreateFilesDto(10))];

		// Act
		sut.AddHierarchy(hierarchy);

		// Assert
		sut.Hierarchy
			.Should()
			.Contain(hierarchy);
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.ClosePopup" />.
	/// </summary>
	[Test]
	public void ClosePopup_Closes_Popup()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.IsPopupFixed = true;

		sut.IsPopupOpen = true;

		// Act
		sut.ClosePopup();

		// Assert
		sut.IsPopupFixed
			.Should()
			.BeFalse();

		sut.IsPopupOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.DisplayCopyHistory" />.
	/// </summary>
	[AvaloniaTest]
	public void DisplayCopyHistory_Displays_CopyHistory_View_In_Popup_Panel()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			using AutoMock mock = AutoMock.GetLoose();

			viewFactory
				.CreateUserControl<CopyHistoryView>()
				.Returns(mock.Create<CopyHistoryView>());

			builder.RegisterInstance(viewFactory);
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		sut.DisplayCopyHistory();

		// Assert
		sut.ShowFavorites
			.Should()
			.BeFalse();

		sut.PopupContent
			.Should()
			.BeOfType<CopyHistoryView>();

		sut.IsPopupOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.DisplayFavorites" />.
	/// </summary>
	[Test]
	public void DisplayFavorites_Displays_Favorites_View_In_Popup_Panel()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			using AutoMock mock = AutoMock.GetLoose();

			viewFactory
				.CreateUserControl<SelectedFavoritesView>()
				.Returns(mock.Create<SelectedFavoritesView>());

			builder.RegisterInstance(viewFactory);
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.ShowContentCopyHistory = true;

		sut.IsPopupOpen = false;

		// Act
		sut.DisplayFavorites();

		// Assert
		sut.ShowContentCopyHistory
			.Should()
			.BeFalse();

		sut.PopupContent
			.Should()
			.BeOfType<SelectedFavoritesView>();

		sut.IsPopupOpen
			.Should()
			.BeTrue();
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

		const int count = 10;

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
			.CopyHistory
			.AddRange(TestUtils.CreateGuids(count));

		// Act
		sut.Dispose();

		// Assert
		sut.PopupContent
			.Should()
			.BeNull();

		sut.FavoritesSettings.Categories
			.Should()
			.BeEmpty();

		sut.FavoritesSettings.OrderedCategories
			.Should()
			.BeEmpty();

		sut.FavoritesSettings.SelectedPairs
			.Should()
			.BeEmpty();

		sut.CopyHistorySettings.CopyHistory
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
			OrderedCategories = [.. TestUtils.CreateGuids(10)],
			SelectedCategoryId = Guid.NewGuid(),
			SelectedPairs = [.. TestUtils.CreateCategoryFavoritePairs(10)]
		};

		CopyHistoryViewSettings copyHistorySettings = new()
		{
			CopyHistory = [.. TestUtils.CreateGuids(10)],
			SelectedCopyHistoryItemId = Guid.NewGuid()
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

		sut.CopyHistorySettings.SelectedCopyHistoryItemId
			.Should()
			.Be(copyHistorySettings.SelectedCopyHistoryItemId);

		sut.CopyHistorySettings.CopyHistory
			.Should()
			.Contain(copyHistorySettings.CopyHistory);
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.SaveCopyHistory" />.
	/// </summary>
	[AvaloniaTest]
	public void SaveCopyHistory_Saves_Copy_History_View_Properties_In_Settings()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		CopyHistoryView view = mock.Create<CopyHistoryView>();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(10)];

		view
			.ViewModel
			.AddTestCopyHistory(items);

		view
			.ViewModel
			.SelectedItem = items[0];

		sut.PopupContent = view;

		// Act
		sut.SaveCopyHistory();

		// Assert
		sut.CopyHistorySettings.SelectedCopyHistoryItemId
			.Should()
			.Be(items[0].Id);
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.SaveFavorites" />.
	/// </summary>
	[Test]
	public void SaveFavorites_Saves_Favorites_View_Properties_In_Settings()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.PopupContent = GetSelectedFavoritesViewMock(mock);

		// Act
		sut.SaveFavorites();

		// Assert
		sut.FavoritesSettings.NavigationColumnWidth
			.Should()
			.NotBe(default);

		sut.FavoritesSettings.SelectedCategoryId
			.Should()
			.NotBeEmpty();

		sut.FavoritesSettings.SelectedPairs
			.Should()
			.NotBeEmpty();

		sut.FavoritesSettings.OrderedCategories
			.Should()
			.NotBeEmpty();
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

			viewLauncher
				.ConfigureEditorWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>())
				.Returns(mock.Create<EditorWindow>());

			builder.RegisterInstance(viewLauncher);
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.IsShutdown
			.Should()
			.BeTrue();

		sut.ShowContentCopyHistory = true;

		sut.ShowFavorites = true;

		// Act
		await sut
			.ShowInEditorAsync(null, default)
			.ConfigureAwait(false);

		// Assert
		sut.IsShutdown
			.Should()
			.BeFalse();

		sut.ShowContentCopyHistory
			.Should()
			.BeFalse();

		sut.ShowFavorites
			.Should()
			.BeFalse();

		viewLauncher
			.Received()
			.ConfigureEditorWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.UpdateCopyHistory" />.
	/// </summary>
	[Test]
	public void UpdateCopyHistory_Inserts_New_Value_To_Top()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.CopyHistorySettings
			.CopyHistory
			.AddRange(TestUtils.CreateGuids(10));

		Guid value = Guid.NewGuid();

		// Act
		sut.UpdateCopyHistory(value);

		// Assert
		sut.CopyHistorySettings.CopyHistory[0]
			.Should()
			.Be(value);
	}

	/// <summary>
	/// Test of <see cref="FavoritesViewModel.UpdateCopyHistory" />.
	/// </summary>
	[Test]
	public void UpdateCopyHistory_Moves_Existing_Value_To_Top()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.CopyHistorySettings
			.CopyHistory
			.AddRange(TestUtils.CreateGuids(10));

		Guid value = sut
			.CopyHistorySettings
			.CopyHistory
			.Last();

		// Act
		sut.UpdateCopyHistory(value);

		// Assert
		sut.CopyHistorySettings.CopyHistory[0]
			.Should()
			.Be(value);
	}
	#endregion

	#region Service
	/// <summary>
	/// Prepares mock of <see cref="SelectedFavoritesView" />.
	/// </summary>
	private static SelectedFavoritesView GetSelectedFavoritesViewMock(AutoMock mock)
	{
		SelectedFavoritesView view = mock.Create<SelectedFavoritesView>();

		view
			.ViewModel
			.NavigationColumnWidth = new(TestUtils.CreateRandomDouble(100.0, 300.0));

		view
			.ViewModel
			.SelectedCategory = TestUtils.CreateFavoriteCategory();

		view
			.ViewModel
			.SelectedPairs
			.AddRange(TestUtils.CreateCategoryFavoritePairs(10));

		view
			.ViewModel
			.OrderedCategories
			.AddRange(TestUtils.CreateGuids(10));

		return view;
	}
	#endregion
}
