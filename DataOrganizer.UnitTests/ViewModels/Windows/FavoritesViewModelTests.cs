using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Enums.Views;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Encryption;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using NSubstitute;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(FavoritesViewModel)}"" type")]
internal class FavoritesViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="FavoritesViewModel.ClosePopupByEsc" />: unfixes and closes the popup.
	/// </summary>
	[Test]
	public void ClosePopupByEsc_Closes_Popup()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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
	/// <see cref="FavoritesViewModel(Application, IAppSettingsStore, IContentVisibility, ICopyHistory, IDialogService, IDispatcherAccessor, IExecutingFiles, ILogger, IMessenger, INotificationService, ISessionEditorTabs, ISessionHierarchy, ITaskExceptionHandler, IViewLauncher)" />:
	/// a new favorites window leaves the running hotkey tracking alone.
	/// </summary>
	[Test]
	public void Constructor_Leaves_The_Hotkey_Tracking_Alone()
	{
		// Arrange
		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			hook
				.IsRunning
				.Returns(true);

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings(trackHotkeys: true));

			// The hook is a lazy singleton that an earlier window has already used.
			Lazy<IKeyboardInputHook> lazyHook = new(() => hook);

			_ = lazyHook.Value;

			builder.RegisterInstance(lazyHook);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		// Act
		_ = mock.Create<FavoritesViewModel>();

		// Assert
		hook
			.DidNotReceiveWithAnyArgs()
			.StopTrackingAsync();

		hook
			.DidNotReceiveWithAnyArgs()
			.StartTrackingAsync();
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: clears the collections of the favorites settings.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.PopupContent = new();

		const int count = 5;

		sut
			.FavoritesSettings
			.Categories
			.AddRange(FavoriteFactory.CreateFavoriteCategories(count));

		sut
			.FavoritesSettings
			.OrderedCategoryIds
			.AddRange(RandomValues.CreateGuids(count));

		sut
			.FavoritesSettings
			.SelectedPairs
			.AddRange(FavoriteFactory.CreateFavoriteSelections(count));

		// Act
		sut.Dispose();

		// Assert
		sut.FavoritesSettings.Categories
			.Should()
			.BeEmpty();

		sut.FavoritesSettings.OrderedCategoryIds
			.Should()
			.BeEmpty();

		sut.FavoritesSettings.SelectedPairs
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the window disposes the list of the shown copy history.
	/// </summary>
	[Test]
	public void Dispose_Disposes_The_Shown_Copy_History_List()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<CopyHistory>()
				.As<ICopyHistory>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		CopyHistoryViewModel copyHistoryList = mock.Create<CopyHistoryViewModel>();

		// The view of the popup hands its view model over once loaded.
		sut
			.CopyHistoryDisplayedCommand
			.Execute(copyHistoryList);

		// Act
		sut.Dispose();

		// Assert
		copyHistoryList.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.FavoritesSettings" />: a new favorites window gathers the favorite files of the tree of
	/// the work session into categories.
	/// </summary>
	[Test]
	public void FavoritesSettings_Holds_The_Favorites_Of_The_Session_Tree()
	{
		// Arrange
		FileDto favorite = ItemDtoFactory.CreateFileDto();

		favorite.IsFavorite = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			session
				.Hierarchy
				.AddRange([favorite, ItemDtoFactory.CreateFileDto()]);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		// Act
		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Assert
		sut.FavoritesSettings.Categories.SelectMany(static x => x.Children)
			.Should()
			.Equal(favorite);
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.Initialize" />: applies the popup size and the favorites settings.
	/// </summary>
	[AvaloniaTest]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		int positiveValue = RandomValues.CreateInt(100, 300);

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
			OrderedCategoryIds = [.. RandomValues.CreateGuids(5)],
			SelectedCategoryId = Guid.NewGuid(),
			SelectedPairs = [.. FavoriteFactory.CreateFavoriteSelections(5)]
		};

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		sut.Initialize(windowSettings, favoritesSettings);

		// Assert
		sut.PopupWidth
			.Should()
			.Be(windowSettings.PopupWidth);

		sut.PopupHeight
			.Should()
			.Be(windowSettings.PopupHeight);

		// The categories come from the hierarchy, not from the settings being applied.
		sut.FavoritesSettings
			.Should()
			.BeEquivalentTo(favoritesSettings, static options => options.Excluding(x => x.Categories));
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.IsShowOnHoverEnabled" />: is initialized from the application settings.
	/// </summary>
	[Test]
	public void IsShowOnHoverEnabled_Is_Initialized_From_The_Settings()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		settings.ShowFavoritesOnHover = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		// Act
		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Assert
		sut.IsShowOnHoverEnabled
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewModelBase.Receive(SessionAutoLockedMessage)" />: the favorites window hides the contents as well.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Hides_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		WorkSession session = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.Hierarchy
			.AddRange([file]);

		session.EditorTabs = new()
		{
			Files = [file],
			PreviousFile = null,
			SelectedFile = file
		};

		// Act
		messenger.Send(new SessionAutoLockedMessage());

		await Task.WhenAll([.. scheduled]);

		// Assert
		file.IsEditing
			.Should()
			.BeFalse();

		session.EditorTabs
			.Should()
			.BeNull();

		contentVisibility
			.Received(1)
			.HideAllContents(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowInEditorAsync" />: configures and shows the editor window.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowInEditorAsync_Shows_Editor_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		EditorWindow? editorWindow = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

			editorWindow = windowMock.Create<EditorWindow>();

			viewLauncher
				.CreateEditorWindow()
				.Returns(editorWindow);

			builder.RegisterInstance(viewLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		await sut.ShowInEditorAsync(default, new());

		// Closed here, otherwise its dialog host stays in the list every headless test shares.
		editorWindow?.Close();

		// Assert
		sut.IsShutdown
			.Should()
			.BeFalse();

		viewLauncher
			.Received(1)
			.CreateEditorWindow();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowPopupOnHover" />: opens the popup with the restored content
	/// without fixing it.
	/// </summary>
	[Test]
	public void ShowPopupOnHover_Opens_Popup()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.IsShowOnHoverEnabled = true;

		// Act
		sut.ShowPopupOnHover();

		// Assert
		sut.IsPopupOpen
			.Should()
			.BeTrue();

		sut.PopupContent
			.Should()
			.Be(FavoritesPopupContentKind.Favorites);

		sut.IsPopupFixed
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowPopupOnHoverCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void ShowPopupOnHoverCommand_CanExecute_Returns_False_When_Disabled_In_Settings()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		bool canExecute = sut
			.ShowPopupOnHoverCommand
			.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowPopupOnHoverCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void ShowPopupOnHoverCommand_CanExecute_Returns_False_When_Popup_Fixed()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.IsShowOnHoverEnabled = true;

		// Act
		sut.IsPopupFixed = true;

		bool canExecute = sut
			.ShowPopupOnHoverCommand
			.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowPopupOnHoverCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void ShowPopupOnHoverCommand_CanExecute_Returns_False_When_Popup_Opened()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.IsShowOnHoverEnabled = true;

		// Act
		sut.IsPopupOpen = true;

		bool canExecute = sut
			.ShowPopupOnHoverCommand
			.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowPopupOnHoverCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void ShowPopupOnHoverCommand_CanExecute_Returns_True_When_Enabled_And_Popup_Closed()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		sut.IsShowOnHoverEnabled = true;

		bool canExecute = sut
			.ShowPopupOnHoverCommand
			.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.BeTrue();
	}
	#endregion
}
