using AppInstance.Interfaces;
using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using Clipboard.Interfaces;
using Clipboard.Services;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Views;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Favorites;
using DataOrganizer.Messages.Updates;
using DataOrganizer.Services.Dialogs;
using DataOrganizer.Services.Encryption;
using DataOrganizer.Services.Execution;
using DataOrganizer.Services.Runtime;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataProtection.Interfaces;
using DataProtection.Messages;
using Diagnostics.Interfaces;
using DialogHostAvalonia;
using EditorMessages.Messages;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using Serilog;
using Settings.Dto;
using Settings.Interfaces;
using Settings.UnitTests.Factories;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using Threading.Interfaces;
using Threading.UnitTests.Fakes;

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
	/// <see cref="FavoritesViewModel(IAppSettingsStore, IConfirmationPrompt, ICopyHistory, IDialogCloser, IDispatcherAccessor, IExecutingFiles, ILogger, IMainWindowLauncher, IMessenger, IProtectionScenarios, ISecondaryWindowLauncher, ISessionHierarchy, ITaskExceptionHandler, IViewModelFactory)" />:
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
	/// <see cref="FavoritesViewModel(IAppSettingsStore, IConfirmationPrompt, ICopyHistory, IDialogCloser, IDispatcherAccessor, IExecutingFiles, ILogger, IMainWindowLauncher, IMessenger, IProtectionScenarios, ISecondaryWindowLauncher, ISessionHierarchy, ITaskExceptionHandler, IViewModelFactory)" />:
	/// the clipboard history is on as the settings track it.
	/// </summary>
	[Test]
	public void Constructor_Takes_The_Clipboard_History_From_The_Settings([Values] bool isTracked)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			AppSettings settings = SettingsFactory.CreateSettings();

			settings.TrackClipboardHistory = isTracked;

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
		sut.IsClipboardHistoryEnabled
			.Should()
			.Be(isTracked);
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
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		CopyHistoryViewModel copyHistoryList = sut.CopyHistoryList!;

		// Act
		sut.Dispose();

		// Assert
		copyHistoryList.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the window disposes the favorites its popup shows.
	/// </summary>
	[Test]
	public void Dispose_Disposes_The_Shown_Favorites()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.ShowFavorites();

		SelectedFavoritesViewModel favorites = sut.SelectedFavorites!;

		// Act
		sut.Dispose();

		// Assert
		favorites.IsDisposed
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
	/// <see cref="FavoritesViewModel.Initialize" />: without a saved popup size and navigation width the popup takes the
	/// default size and half of its width goes to the navigation.
	/// </summary>
	[AvaloniaTest]
	public void Initialize_Gives_The_Default_Popup_Size_And_Navigation_Width_Without_Saved_Ones()
	{
		// Arrange
		FavoritesWindowSettings windowSettings = new()
		{
			PopupHeight = 0.0,
			PopupWidth = 0.0,
			X = 10,
			Y = 10
		};

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		sut.Initialize(windowSettings, new FavoritesViewSettings());

		// Assert
		sut.PopupHeight
			.Should()
			.Be(250.0);

		sut.PopupWidth
			.Should()
			.Be(sut.PopupHeight * 2.0);

		sut.FavoritesSettings.NavigationColumnWidth
			.Should()
			.Be(sut.PopupWidth / 2.0);
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
	/// <see cref="FavoritesViewModel.IsPopupOpen" />: closing the popup closes the dialog of the main dialog host.
	/// </summary>
	[AvaloniaTest]
	public void IsPopupOpen_Closes_The_Dialog_Of_The_Main_Host_When_The_Popup_Closes()
	{
		// Arrange
		Window window = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([window]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();

			builder
				.RegisterType<DesktopAccessor>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<DialogCloser>()
				.As<IDialogCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		window.Show();

		Task dialogClosed = DialogHost.Show(new TextBlock(), DialogHostIdentifiers.Main);

		Dispatcher.UIThread.RunJobs();

		sut.IsPopupOpen = true;

		// Act
		sut.IsPopupOpen = false;

		bool isClosed = dialogClosed.IsCompleted;

		// Closed before the assertion: a closed window takes its host out of the list every headless test shares.
		window.Close();

		// Assert
		isClosed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.IsPopupOpen" />: closing the popup disposes the favorites it showed.
	/// </summary>
	[Test]
	public void IsPopupOpen_Disposes_The_Favorites_When_The_Popup_Closes()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.ShowFavorites();

		SelectedFavoritesViewModel favorites = sut.SelectedFavorites!;

		// Act
		sut.IsPopupOpen = false;

		// Assert
		favorites.IsDisposed
			.Should()
			.BeTrue();

		sut.SelectedFavorites
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.IsPopupOpen" />: closing the popup saves the category chosen in the favorites it shows.
	/// </summary>
	[AvaloniaTest]
	public void IsPopupOpen_Saves_The_Chosen_Category_When_The_Popup_Closes()
	{
		// Arrange
		FavoriteCategory[] categories = [.. FavoriteFactory.CreateFavoriteCategories(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.FavoritesSettings
			.Categories
			.AddRange(categories);

		sut.ShowFavorites();

		SelectedFavoritesViewModel favorites = sut.SelectedFavorites!;

		// The stored selection comes back through the dispatcher before the choice.
		Dispatcher.UIThread.RunJobs();

		favorites.SelectedCategory = categories[1];

		// Act
		sut.IsPopupOpen = false;

		// Assert
		sut.FavoritesSettings.SelectedCategoryId
			.Should()
			.Be(categories[1].Id);
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.IsPopupOpen" />: closing the popup saves the width of the navigation column of the
	/// favorites it shows.
	/// </summary>
	[AvaloniaTest]
	public void IsPopupOpen_Saves_The_Navigation_Column_Width_When_The_Popup_Closes()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut.ShowFavorites();

		sut.SelectedFavorites!.NavigationColumnWidth = 222.0;

		// Act
		sut.IsPopupOpen = false;

		// Assert
		sut.FavoritesSettings.NavigationColumnWidth
			.Should()
			.Be(222.0);
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
	/// <see cref="FavoritesViewModel.Receive(ConfirmUpdateMessage)" />: the question is asked over the favorites shown in
	/// the popup, which closes again with the answer.
	/// </summary>
	[Test]
	public async Task Receive_ConfirmUpdate_Asks_Over_The_Favorites_And_Closes_The_Popup_Again()
	{
		// Arrange
		const string text = "Version 0.2.0 is available.";

		FavoritesPopupContentKind askedOver = FavoritesPopupContentKind.None;

		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		IMessenger messenger = new StrongReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(messenger);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// The popup content is read while the question is open: closing the popup clears it.
		confirmationPrompt
			.RequestYesNoAsync(text, Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(_ =>
			{
				askedOver = sut.PopupContent;

				return true;
			});

		// Act
		bool answer = await messenger.Send(new ConfirmUpdateMessage(text, CancellationToken.None));

		// Assert
		answer
			.Should()
			.BeTrue();

		askedOver
			.Should()
			.Be(FavoritesPopupContentKind.Favorites);

		sut.IsPopupOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.Receive(SessionAutoLockedMessage)" />: the favorites window closes the open files and hides
	/// the contents as well.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Hides_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		FileDto executingFile = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		WorkSession session = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(engine);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.Hierarchy
			.AddRange([file, executingFile]);

		await mock
			.Create<IExecutingFiles>()
			.ExecuteAsync(new()
			{
				Contents = [],
				File = executingFile,
				IsReadOnly = false
			});

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

		executingFile.IsExecuting
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
	/// <see cref="FavoritesViewModel.Receive(SessionAutoLockedMessage)" />: the files of the editor tabs that wait in the work
	/// session are no longer marked as edited.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Unmarks_The_Files_Of_The_Waiting_Tabs()
	{
		// Arrange
		FileDto first = ItemDtoFactory.CreateFileDto(isEditing: true);

		FileDto second = ItemDtoFactory.CreateFileDto(isEditing: true);

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			WorkSession session = new()
			{
				EditorTabs = new()
				{
					Files = [first, second],
					PreviousFile = first,
					SelectedFile = second
				}
			};

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.Hierarchy
			.AddRange([first, second]);

		// Act
		messenger.Send(new SessionAutoLockedMessage());

		await Task.WhenAll([.. scheduled]);

		// Assert
		first.IsEditing
			.Should()
			.BeFalse();

		second.IsEditing
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.Receive(ShowFavoritesMessage)" />: the favorites open in the popup.
	/// </summary>
	[AvaloniaTest]
	public void Receive_ShowFavorites_Opens_The_Favorites()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		messenger.Send(new ShowFavoritesMessage());

		// Assert
		sut.PopupContent
			.Should()
			.Be(FavoritesPopupContentKind.Favorites);
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.Receive(ShowInEditorMessage)" />: the window gives way to an editor that shows the
	/// object.
	/// </summary>
	[Test]
	public void Receive_ShowInEditor_Switches_To_An_Editor_With_The_Object()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		IMessenger messenger = new StrongReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(mainWindowLauncher);

			builder.RegisterInstance(messenger);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		_ = mock.Create<FavoritesViewModel>();

		// Act
		messenger.Send(new ShowInEditorMessage(id));

		// Assert
		mainWindowLauncher
			.Received(1)
			.SwitchToEditor(id);
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.SaveContent" />: a second save keeps the stored order of the categories, since the
	/// favorites stay alive while the popup shows them.
	/// </summary>
	[AvaloniaTest]
	public void SaveContent_Twice_Keeps_The_Stored_Order()
	{
		// Arrange
		FavoriteCategory[] categories = [.. FavoriteFactory.CreateFavoriteCategories(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.FavoritesSettings
			.Categories
			.AddRange(categories);

		sut
			.FavoritesSettings
			.OrderedCategoryIds
			.AddRange([categories[1].Id, categories[0].Id]);

		sut.ShowFavorites();

		Dispatcher.UIThread.RunJobs();

		sut.SaveContent();

		// Act
		sut.SaveContent();

		// Assert
		sut.FavoritesSettings.OrderedCategoryIds
			.Should()
			.Equal(categories[1].Id, categories[0].Id);
	}

	/// <summary>
	/// <see cref="ViewModelBase.ShowCustomClipboardCommand" />: the clipboard history opens.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowCustomClipboardCommand_Shows_The_Clipboard_History()
	{
		// Arrange
		ISecondaryWindowLauncher secondaryWindowLauncher = Substitute.For<ISecondaryWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(secondaryWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		await sut
			.ShowCustomClipboardCommand
			.ExecuteAsync(null);

		// Assert
		await secondaryWindowLauncher
			.Received(1)
			.ShowClipboardLogWindowAsync();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowEditorCommand" />: the window gives way to the editor.
	/// </summary>
	[Test]
	public void ShowEditorCommand_Switches_To_The_Editor()
	{
		// Arrange
		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		// Act
		sut
			.ShowEditorCommand
			.Execute(null);

		// Assert
		mainWindowLauncher
			.Received(1)
			.SwitchToEditor(default);
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowFavorites" />: the switch of the popup to the favorites disposes the list of the
	/// copy history it showed.
	/// </summary>
	[Test]
	public void ShowFavorites_Disposes_The_Shown_Copy_History_List()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		CopyHistoryViewModel copyHistoryList = sut.CopyHistoryList!;

		// Act
		sut.ShowFavorites();

		// Assert
		copyHistoryList.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowFavorites" />: the favorites show the categories in the stored order and leave the
	/// categories of the settings as they are.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Leaves_The_Order_Of_The_Settings_Categories()
	{
		// Arrange
		FavoriteCategory[] categories = [.. FavoriteFactory.CreateFavoriteCategories(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel sut = mock.Create<FavoritesViewModel>();

		sut
			.FavoritesSettings
			.Categories
			.AddRange(categories);

		sut
			.FavoritesSettings
			.OrderedCategoryIds
			.AddRange([categories[1].Id, categories[0].Id]);

		// Act
		sut.ShowFavorites();

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.SelectedFavorites!.Categories
			.Should()
			.Equal(categories[1], categories[0]);

		sut.FavoritesSettings.Categories
			.Should()
			.Equal(categories);
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
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

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
