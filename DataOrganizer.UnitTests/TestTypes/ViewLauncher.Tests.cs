using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using NSubstitute;
using Shared.Interfaces;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ViewLauncher)}"" type")]
internal class ViewLauncherTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureCustomClipboardWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureCustomClipboardWindow_Applies_Saved_Settings()
	{
		// Arrange
		int positiveValue = TestUtils.CreateRandomInt(100, 300);

		CustomClipboardWindowSettings settings = new()
		{
			Size = new(positiveValue, positiveValue),
			X = 10,
			Y = 10
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			CustomClipboardViewModel viewModel = windowMock.Create<CustomClipboardViewModel>();

			CustomClipboardWindow clipboardWindow = windowMock.Create<CustomClipboardWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.FromFile<CustomClipboardWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<CustomClipboardViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<CustomClipboardWindow>(Arg.Any<object[]>())
				.Returns(clipboardWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		CustomClipboardWindow window = sut.ConfigureCustomClipboardWindow(new Window());

		// Assert
		window.Width
			.Should()
			.Be(positiveValue);

		window.Height
			.Should()
			.Be(positiveValue);

		window.Position
			.Should()
			.Be(new PixelPoint(settings.X, settings.Y));
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureEditorWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureEditorView_Creates_Window_With_Default_Settings_For_The_First_Launch()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		EditorWindow window = sut.ConfigureEditorWindow([], [], []);

		// Assert
		window.Width
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Width);

		window.Height
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Height);

		window.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);

		window.ViewModel.NavigationColumnWidth.Value
			.Should()
			.Be(window.Width / 3.0);
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureEditorWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureEditorView_ViewModel_Should_Be_Initialized()
	{
		// Arrange
		int positiveValue = TestUtils.CreateRandomInt(100, 300);

		EditorWindowSettings settings = new()
		{
			IsReadOnly = true,
			NavigationColumnWidth = positiveValue - 20,
			Size = new(positiveValue, positiveValue),
			WindowState = WindowState.Normal,
			X = positiveValue,
			Y = positiveValue
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.FromFile<EditorWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		EditorWindow window = sut.ConfigureEditorWindow([], [], []);

		// Assert
		window.ViewModel.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureFavoritesWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureFavoritesWindow_Creates_Window_With_Default_Settings_For_The_First_Launch()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		FavoritesWindow window = sut.ConfigureFavoritesWindow([], [], []);

		// Assert
		window.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);

		window.ViewModel.PopupHeight
			.Should()
			.Be(250.0);

		window.ViewModel.PopupWidth
			.Should()
			.Be(window.ViewModel.PopupHeight * 2.0);

		window.ViewModel.FavoritesSettings.NavigationColumnWidth
			.Should()
			.Be(window.ViewModel.PopupWidth / 2.0);

		window.ViewModel.FavoritesSettings.SelectedCategoryId
			.Should()
			.Be(Guid.Empty);
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureFavoritesWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureFavoritesWindow_ViewModel_Should_Be_Initialized()
	{
		// Arrange
		int positiveValue = TestUtils.CreateRandomInt(100, 300);

		FavoritesWindowSettings settings = new()
		{
			PopupHeight = positiveValue,
			PopupWidth = positiveValue,
			X = positiveValue,
			Y = positiveValue
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.FromFile<FavoritesWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		FavoritesWindow window = sut.ConfigureFavoritesWindow([], [], []);

		// Assert
		window.ViewModel.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureMainWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureMainWindow_Configures_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		Window window = sut.ConfigureMainWindow([]);

		// Assert
		window
			.Should()
			.BeOfType<EditorWindow>();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureMainWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureMainWindow_Configures_Editor_If_No_Settings()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		Window window = sut.ConfigureMainWindow([]);

		// Assert
		window
			.Should()
			.BeOfType<EditorWindow>();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureMainWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureMainWindow_Configures_Favorites()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.FromFile<CurrentWindow>(Arg.Any<string>())
				.Returns(CurrentWindow.Favorites);

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		Window window = sut.ConfigureMainWindow([]);

		// Assert
		window
			.Should()
			.BeOfType<FavoritesWindow>();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.SaveCustomClipboardSettings" />.
	/// </summary>
	[AvaloniaTest]
	public void SaveCustomClipboardSettings_Saves_Settings()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(fileSystem));

		// Act
		sut.SaveCustomClipboardSettings(mock.Create<CustomClipboardWindow>());

		// Assert
		fileSystem.Received().SerializeToJsonFile(
			Arg.Any<CustomClipboardWindowSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.SaveEditorSettingsAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task SaveEditorSettingsAsync_Saves_Settings()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(fileSystem));

		// Act
		await sut.SaveEditorSettingsAsync(mock.Create<EditorWindow>());

		// Assert
		fileSystem.Received().SerializeToJsonFile(
			Arg.Any<EditorWindowSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());

		fileSystem.Received().SerializeToJsonFile(
			CurrentWindow.Editor,
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.SaveFavoritesSettingsAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task SaveFavoritesSettingsAsync_Saves_Settings()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(fileSystem));

		FavoritesWindow window = mock.Create<FavoritesWindow>();

		window
			.ViewModel
			.FavoritesSettings
			.Categories
			.AddRange(TestUtils.CreateFavoriteCategories(5));

		window
			.ViewModel
			.FavoritesSettings
			.SelectedPairs
			.AddRange(TestUtils.CreateCategoryFavoritePairs(5));

		// Act
		await sut.SaveFavoritesSettingsAsync(window);

		// Assert
		window.ViewModel.FavoritesSettings.Categories
			.Should()
			.BeEmpty();

		window.ViewModel.FavoritesSettings.OrderedCategories
			.Should()
			.BeEmpty();

		window.ViewModel.FavoritesSettings.SelectedPairs
			.Should()
			.BeEmpty();

		fileSystem.Received().SerializeToJsonFile(
			Arg.Any<FavoritesWindowSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());

		fileSystem.Received().SerializeToJsonFile(
			CurrentWindow.Favorites,
			Arg.Any<string>(),
			Arg.Any<bool>());
	}
	#endregion
}
