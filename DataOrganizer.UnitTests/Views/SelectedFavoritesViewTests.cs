using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Repository.Interfaces.Database;
using Shared.Common;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(SelectedFavoritesView)}"" type")]
internal class SelectedFavoritesViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="SelectedFavoritesView.CategoriesList" />: the list with this id for UI Automation shows the categories.
	/// </summary>
	[AvaloniaTest]
	public void CategoriesList_Shows_The_Categories()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(Application.Current!));

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		// Act
		Window window = Show(viewModel, navigationColumnWidth: 150.0);

		SelectedFavoritesView view = (SelectedFavoritesView)window.Content!;

		window.Close();

		// Assert
		view.CategoriesList.ItemsSource
			.Should()
			.BeSameAs(viewModel.Categories);
	}

	/// <summary>
	/// <see cref="SelectedFavoritesView.CategorySearchInput" />: the input with this id for UI Automation edits the search of
	/// the categories.
	/// </summary>
	[AvaloniaTest]
	public void CategorySearchInput_Edits_The_Category_Search()
	{
		// Arrange
		string search = RandomString.Create(8);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(Application.Current!);

			// The search typed here waits for its pause, which does not come here.
			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		Window window = Show(viewModel, navigationColumnWidth: 150.0);

		SelectedFavoritesView view = (SelectedFavoritesView)window.Content!;

		// Act
		view.CategorySearchInput.Text = search;

		window.Close();

		// Assert
		viewModel.CategorySearch
			.Should()
			.Be(search);
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: a click on the copy button of a favorite highlights it in the
	/// list.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_The_Copy_Button_Highlights_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		FavoriteCategory category = FavoriteFactory.CreateFavoriteCategory();

		category.Children.Add(file);

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

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		viewModel.Initialize(
			navigationColumnWidth: 150.0,
			selectedCategoryId: category.Id,
			categories: [category],
			orderedCategoryIds: [category.Id],
			selectedPairs: []);

		SelectedFavoritesView view = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = view,
			Height = 300.0,
			Width = 600.0
		};

		window.Show();

		await Dispatcher.UIThread.RunJobsUntilAsync(() => FindItem(window, file) is not null);

		ListBoxItem item = FindItem(window, file)!;

		IBrush? background = item.Background;

		Button button = window
			.GetVisualDescendants()
			.OfType<Button>()
			.First(x => x.DataContext == file && x.Command == viewModel.CopyContentCommand);

		Point point = button
			.TranslatePoint(new Rect(button.Bounds.Size).Center, window)!
			.Value;

		// Act
		window.Click(point);

		await Dispatcher.UIThread.RunJobsUntilAsync(() => !ReferenceEquals(item.Background, background));

		IBrush? highlight = item.Background;

		window.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
	}

	/// <summary>
	/// <see cref="SelectedFavoritesView.FavoriteSearchInput" />: the input with this id for UI Automation edits the search of
	/// the favorites.
	/// </summary>
	[AvaloniaTest]
	public void FavoriteSearchInput_Edits_The_Favorite_Search()
	{
		// Arrange
		string search = RandomString.Create(8);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(Application.Current!);

			// The search typed here waits for its pause, which does not come here.
			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		Window window = Show(viewModel, navigationColumnWidth: 150.0);

		SelectedFavoritesView view = (SelectedFavoritesView)window.Content!;

		// Act
		view.FavoriteSearchInput.Text = search;

		window.Close();

		// Assert
		viewModel.FavoriteSearch
			.Should()
			.Be(search);
	}

	/// <summary>
	/// <see cref="SelectedFavoritesView.FavoritesList" />: the list with this id for UI Automation shows the favorites.
	/// </summary>
	[AvaloniaTest]
	public void FavoritesList_Shows_The_Favorites()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(Application.Current!));

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		// Act
		Window window = Show(viewModel, navigationColumnWidth: 150.0);

		SelectedFavoritesView view = (SelectedFavoritesView)window.Content!;

		window.Close();

		// Assert
		view.FavoritesList.ItemsSource
			.Should()
			.BeSameAs(viewModel.Favorites);
	}

	/// <summary>
	/// <see cref="SelectedFavoritesViewModel.NavigationColumnWidth" />: the width a dragged splitter gives the navigation
	/// column comes back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void NavigationColumnWidth_Follows_The_Navigation_Column()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(Application.Current!));

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		Window window = Show(viewModel, navigationColumnWidth: 150.0);

		// Act
		FindNavigationColumn(window).Width = new GridLength(222.0);

		window.Close();

		// Assert
		viewModel.NavigationColumnWidth
			.Should()
			.Be(222.0);
	}

	/// <summary>
	/// <see cref="SelectedFavoritesViewModel.NavigationColumnWidth" />: the width of the view model reaches the navigation
	/// column.
	/// </summary>
	[AvaloniaTest]
	public void NavigationColumnWidth_Reaches_The_Navigation_Column()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(Application.Current!));

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		// Act
		Window window = Show(viewModel, navigationColumnWidth: 150.0);

		GridLength width = FindNavigationColumn(window).Width;

		window.Close();

		// Assert
		width
			.Should()
			.Be(new GridLength(150.0));
	}

	/// <summary>
	/// <see cref="SelectedFavoritesViewModel.NavigationColumnWidth" />: a view that narrows below the navigation column
	/// narrows the column, leaving a margin beside it.
	/// </summary>
	[AvaloniaTest]
	public void Resize_Narrows_The_Navigation_Column()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(Application.Current!));

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		Window window = Show(viewModel, navigationColumnWidth: 300.0);

		// Act
		window.Width = 200.0;

		Dispatcher.UIThread.RunJobs();

		GridLength width = FindNavigationColumn(window).Width;

		window.Close();

		// Assert
		width
			.Should()
			.Be(new GridLength(180.0));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the navigation column of the favorites view a window shows.
	/// </summary>
	private static ColumnDefinition FindNavigationColumn(Window window)
	{
		return window
			.GetVisualDescendants()
			.OfType<SelectedFavoritesView>()
			.Single()
			.GetVisualDescendants()
			.OfType<Grid>()
			.First()
			.ColumnDefinitions[0];
	}

	/// <summary>
	/// Returns the item of the favorites list that shows a file; <c>null</c> while the list shows none.
	/// </summary>
	private static ListBoxItem? FindItem(Window window, FileDto file)
	{
		return window
			.GetVisualDescendants()
			.OfType<ListBoxItem>()
			.FirstOrDefault(x => x.DataContext == file);
	}

	/// <summary>
	/// Shows the favorites view without categories, with a navigation column of a width, in a window 600 wide.
	/// </summary>
	private static Window Show(SelectedFavoritesViewModel viewModel, double navigationColumnWidth)
	{
		viewModel.Initialize(
			navigationColumnWidth: navigationColumnWidth,
			selectedCategoryId: Guid.Empty,
			categories: [],
			orderedCategoryIds: [],
			selectedPairs: []);

		Window window = new()
		{
			Content = new SelectedFavoritesView
			{
				DataContext = viewModel
			},
			Height = 300.0,
			Width = 600.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion

}
