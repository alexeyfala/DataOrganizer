using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using NSubstitute;
using Shared.Common;
using Shared.Interfaces;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ViewLauncher)}"" type")]
internal class ViewLauncherTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureEditorWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureEditorView_Creates_Window_With_Default_Settings_For_The_First_Launch()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMockForEditorView(mock)));

		// Act
		EditorWindow window = sut.ConfigureEditorWindow([]);

		// Assert
		AssertWindowHasDefaultSettings(window);
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

		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMockForEditorView(mock)),
			TypedParameter.From(GetSerializerMockForSettings(settings)));

		// Act
		EditorWindow window = sut.ConfigureEditorWindow([]);

		// Assert
		window.ViewModel.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureEntityCreationView" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureEntityCreationView_ViewModel_Should_Be_Initialized()
	{
		// Arrange
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = false,
			IsFileSelected = false,
			IsFolderSelected = true,
			Name = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMock<EntityCreationView>(mock)),
			TypedParameter.From(GetSerializerMockForSettings(settings)));

		// Act
		EntityCreationView view = sut.ConfigureEntityCreationView();

		// Assert
		view.ViewModel.IsInitialized
			.Should()
			.BeTrue();

		view.ViewModel
			.Should()
			.BeAssignableTo<DefaultButtonViewModelBase>();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureFavoritesWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureFavoritesWindow_Creates_Window_With_Default_Settings_For_The_First_Launch()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMockForFavoritesView(mock)));

		// Act
		FavoritesWindow window = sut.ConfigureFavoritesWindow([]);

		// Assert
		AssertWindowHasDefaultSettings(window);
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

		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMockForFavoritesView(mock)),
			TypedParameter.From(GetSerializerMockForSettings(settings)));

		// Act
		FavoritesWindow window = sut.ConfigureFavoritesWindow([]);

		// Assert
		window.ViewModel.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureKeyValueInputView" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureKeyValueInputView_ViewModel_Should_Apply_Property_Values()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMock<KeyValueInputView>(mock)));

		// Act
		KeyValueInputView view = sut.ConfigureKeyValueInputView(AppUtils.CreateRandomString(10));

		// Assert
		view.ViewModel.IsInitialized
			.Should()
			.BeTrue();

		view.ViewModel
			.Should()
			.BeAssignableTo<DefaultButtonViewModelBase>();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureMainWindow" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureMainWindow_Configures_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMockForEditorView(mock)),
			TypedParameter.From(GetSerializerMockForSettings(CurrentWindow.Editor)));

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
		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMockForEditorView(mock)));

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
		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMockForFavoritesView(mock)),
			TypedParameter.From(GetSerializerMockForSettings(CurrentWindow.Favorites)));

		// Act
		Window window = sut.ConfigureMainWindow([]);

		// Assert
		window
			.Should()
			.BeOfType<FavoritesWindow>();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureMultilineTextEditView" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureMultilineTextEditView_ViewModel_Text_Should_Be_As_Parameter()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMock<MultilineTextEditView>(mock)));

		string text = AppUtils.CreateRandomString(10);

		// Act
		MultilineTextEditView view = sut.ConfigureMultilineTextEditView(text);

		// Assert
		view.ViewModel.Text
			.Should()
			.Be(text);

		view.ViewModel
			.Should()
			.BeAssignableTo<DefaultButtonViewModelBase>();
	}

	/// <summary>
	/// Test of <see cref="ViewLauncher.ConfigureYesNoQuestionBox" />.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureYesNoQuestionBox_ViewModel_Text_Should_Be_As_Parameter()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ViewLauncher sut = mock.Create<ViewLauncher>(
			TypedParameter.From(GetViewFactoryMock<YesNoQuestionBox>(mock)));

		string text = AppUtils.CreateRandomString(10);

		// Act
		YesNoQuestionBox view = sut.ConfigureYesNoQuestionBox(text);

		// Assert
		view.ViewModel.Text
			.Should()
			.Be(text);

		view.ViewModel
			.Should()
			.BeAssignableTo<DefaultButtonViewModelBase>();
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
		await sut
			.SaveEditorSettingsAsync(mock.Create<EditorWindow>())
			.ConfigureAwait(false);

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
			.AddRange(TestUtils.CreateFavoriteCategories(10));

		window
			.ViewModel
			.FavoritesSettings
			.SelectedPairs
			.AddRange(TestUtils.CreateCategoryFavoritePairs(10));

		// Act
		await sut
			.SaveFavoritesSettingsAsync(window)
			.ConfigureAwait(false);

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

	#region Service
	/// <summary>
	/// Asserts that <see cref="EditorWindow" /> has default settings.
	/// </summary>
	private static void AssertWindowHasDefaultSettings(EditorWindow window)
	{
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
	/// Asserts that <see cref="FavoritesWindow" /> has default settings.
	/// </summary>
	private static void AssertWindowHasDefaultSettings(FavoritesWindow window)
	{
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
	/// Prepares mock of <see cref="IJsonSerializerWrapper.FromFile{T}" /> to return settings.
	/// </summary>
	private static IJsonSerializerWrapper GetSerializerMockForSettings<T>(T settings)
	{
		IJsonSerializerWrapper jsonSerializer = Substitute.For<IJsonSerializerWrapper>();

		jsonSerializer
			.FromFile<T>(Arg.Any<string>())
			.Returns(settings);

		return jsonSerializer;
	}

	/// <summary>
	/// Prepares mock of <see cref="IViewFactory.CreateUserControl{T}" /> to return <see cref="UserControl" />.
	/// </summary>
	private static IViewFactory GetViewFactoryMock<T>(AutoMock mock) where T : UserControl
	{
		IViewFactory factory = Substitute.For<IViewFactory>();

		factory
			.CreateUserControl<T>()
			.Returns(mock.Create<T>());

		return factory;
	}

	/// <summary>
	/// Prepares mock of <see cref="IViewFactory.CreateWindow{T}" /> to return <see cref="EditorWindow" />.
	/// </summary>
	private static IViewFactory GetViewFactoryMockForEditorView(AutoMock mock)
	{
		IViewFactory factory = Substitute.For<IViewFactory>();

		factory
			.CreateWindow<EditorWindow>()
			.Returns(mock.Create<EditorWindow>());

		return factory;
	}

	/// <summary>
	/// Prepares mock of <see cref="IViewFactory.CreateWindow{T}" /> to return <see cref="FavoritesWindow" />.
	/// </summary>
	private static IViewFactory GetViewFactoryMockForFavoritesView(AutoMock mock)
	{
		IViewFactory factory = Substitute.For<IViewFactory>();

		factory
			.CreateWindow<FavoritesWindow>()
			.Returns(mock.Create<FavoritesWindow>());

		return factory;
	}
	#endregion
}
