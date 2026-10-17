using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using NSubstitute;
using Repository.Interfaces.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(SelectedFavoritesViewModel)}"" type")]
internal class SelectedFavoritesViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: a file copied from the list goes into the copy history,
	/// and a shown list moves it at its next showing.
	/// </summary>
	[Test]
	public void CopyContentCommand_Puts_The_File_Into_The_Copy_History()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();
		});

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		// Act
		sut
			.CopyContentCommand
			.Execute(file);

		// Assert
		copyHistory
			.Received(1)
			.Add(file, false);
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: the copied file becomes the selected favorite.
	/// </summary>
	[Test]
	public void CopyContentCommand_Selects_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose();

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		// Act
		sut
			.CopyContentCommand
			.Execute(file);

		// Assert
		sut.SelectedFavorite
			.Should()
			.BeSameAs(file);
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: it clears all collections and selection properties.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SynchronizationContext.SetSynchronizationContext(null);

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		const int count = 5;

		List<FavoriteCategory> categories = [.. FavoriteFactory.CreateFavoriteCategories(count)];

		Guid selectedCategoryId = categories[0].Id;

		// The favorites of the selected category are what fills the favorites collection.
		categories[0]
			.Children
			.AddRange(ItemDtoFactory.CreateFileDtos(count));

		sut.Initialize(
			navigationColumnWidth: RandomValues.CreateDouble(100.0, 300.0),
			selectedCategoryId: selectedCategoryId,
			categories: categories,
			orderedCategoryIds: [.. categories.Select(x => x.Id)],
			selectedPairs: [.. FavoriteFactory.CreateFavoriteSelections(count)]);

		sut.IsFavoritesEmpty
			.Should()
			.BeFalse();

		// Act
		sut.Dispose();

		// Assert
		sut.IsCategoriesEmpty
			.Should()
			.BeTrue();

		sut.IsFavoritesEmpty
			.Should()
			.BeTrue();

		sut.OrderedCategoryIds
			.Should()
			.BeEmpty();

		sut.SelectedPairs
			.Should()
			.BeEmpty();

		sut.SelectedCategory
			.Should()
			.BeNull();

		sut.SelectedFavorite
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SelectedFavoritesViewModel.Initialize" />: the stored order drops the ids of the categories that are gone,
	/// also when two of them stand in a row.
	/// </summary>
	[Test]
	public void Initialize_Drops_The_Ids_Of_Categories_That_Are_Gone()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SynchronizationContext.SetSynchronizationContext(null);

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		List<FavoriteCategory> categories = [.. FavoriteFactory.CreateFavoriteCategories(2)];

		// Act
		sut.Initialize(
			navigationColumnWidth: RandomValues.CreateDouble(100.0, 300.0),
			selectedCategoryId: categories[0].Id,
			categories: categories,
			orderedCategoryIds: [categories[1].Id, Guid.NewGuid(), Guid.NewGuid(), categories[0].Id],
			selectedPairs: []);

		// Assert
		sut.OrderedCategoryIds
			.Should()
			.Equal(categories[1].Id, categories[0].Id);
	}

	/// <summary>
	/// <see cref="SelectedFavoritesViewModel.Initialize" />: it populates the collections and selection properties from the supplied data.
	/// </summary>
	[Test]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SynchronizationContext.SetSynchronizationContext(null);

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		const int count = 5;

		List<FavoriteCategory> categories = [.. FavoriteFactory.CreateFavoriteCategories(count)];

		// Act
		sut.Initialize(
			navigationColumnWidth: RandomValues.CreateDouble(100.0, 300.0),
			selectedCategoryId: categories[0].Id,
			categories: categories,
			orderedCategoryIds: [.. categories.OrderBy(x => x.Name).Select(x => x.Id)],
			selectedPairs: [.. FavoriteFactory.CreateFavoriteSelections(count)]);

		// Assert
		sut.NavigationColumnWidth.Value
			.Should()
			.NotBe(default);

		sut.SelectedCategory
			.Should()
			.NotBeNull();

		sut.IsCategoriesEmpty
			.Should()
			.BeFalse();

		sut.OrderedCategoryIds
			.Should()
			.NotBeEmpty();

		sut.SelectedPairs
			.Should()
			.NotBeEmpty();
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.ShowInEditorCommand" />: the object is shown in the editor of the main window.
	/// </summary>
	[Test]
	public void ShowInEditorCommand_Shows_The_Object_In_The_Editor()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(mainWindowLauncher));

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		// Act
		sut
			.ShowInEditorCommand
			.Execute(id);

		// Assert
		mainWindowLauncher
			.Received(1)
			.ShowInEditor(id);
	}

	/// <summary>
	/// <see cref="SelectedFavoritesViewModel.SizeChangedCommand" />: the navigation column narrows below the view width
	/// when the view shrinks.
	/// </summary>
	[Test]
	public void SizeChangedCommand_Clamps_The_Navigation_Column_To_Less_Than_The_View_Width()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SynchronizationContext.SetSynchronizationContext(null);

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		const double viewWidth = 250.0;

		sut.NavigationColumnWidth = new GridLength(viewWidth * 2);

		// Act
		sut.SizeChangedCommand.Execute(new SizeChangedEventArgs(null, null, default, new Size(viewWidth, viewWidth)));

		// Assert
		sut.NavigationColumnWidth.Value
			.Should()
			.BeLessThan(viewWidth);
	}
	#endregion
}
