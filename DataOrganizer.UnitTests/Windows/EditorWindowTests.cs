using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using Clipboard.Interfaces;
using Clipboard.Services;
using DataOrganizer.Controls;
using DataOrganizer.Enums.Views;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Hierarchy;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Hierarchy;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using EditorMessages.Messages;
using Entities.Enums;
using Entities.Models;
using FileContents.Enums;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using MapsterMapper;
using Material.Styles.Controls;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Settings.Dto;
using Settings.Enums;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(EditorWindow)}"" type")]
internal class EditorWindowTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditorViewModel.AddAsync" />: the folder an object is added to is shown expanded and saved so.
	/// </summary>
	[AvaloniaTest]
	public async Task AddAsync_Expands_The_Folder_Of_The_Object_Added()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IMapper mapper = Substitute.For<IMapper>();

			dbAccess
				.AddEntityAsync(Arg.Any<AddEntityParameters>(), Arg.Any<CancellationToken>())
				.Returns(Substitute.For<ExplorerItemBase>());

			mapper
				.Map<ExplorerItemBase, ExplorerItemDtoBase>(Arg.Any<ExplorerItemBase>())
				.Returns(ItemDtoFactory.CreateFileDto());

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<HierarchyEditor>()
				.As<IHierarchyEditor>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(folder);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		await viewModel.AddAsync(
			RandomString.Create(10),
			EntityKind.File,
			folder);

		Dispatcher.UIThread.RunJobs();

		bool isExpanded = sut.FindTreeItem(folder) is { IsExpanded: true };

		sut.Close();

		// Assert
		isExpanded
			.Should()
			.BeTrue();

		await propertyWriter
			.Received(1)
			.UpdateIsExpandedAsync(folder.Id, true, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorWindow" />: a click on the copy history button opens the side sheet with the focus inside it, so that
	/// Escape reaches the sheet at once.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Copy_History_Button_Moves_The_Focus_Into_The_Side_Sheet()
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

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Button button = sut
			.GetVisualDescendants()
			.OfType<Button>()
			.Single(x => ReferenceEquals(x.Command, viewModel.ShowCopyHistoryCommand));

		// Act
		sut.Click(sut.CenterOf(button));

		Dispatcher.UIThread.RunJobs();

		bool isOpened = viewModel.IsRightSideSheetOpened;

		bool isFocusInside = sut
			.GetVisualDescendants()
			.OfType<SideSheet>()
			.Single()
			.IsKeyboardFocusWithin;

		sut.Close();

		// Assert
		isOpened
			.Should()
			.BeTrue();

		isFocusInside
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: a click on the executing files button while the side sheet shows the copy history moves
	/// the focus into the sheet again, so that Escape reaches it at once.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Executing_Files_Button_Moves_The_Focus_Into_The_Open_Side_Sheet()
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

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		Button button = sut
			.GetVisualDescendants()
			.OfType<Button>()
			.Single(x => ReferenceEquals(x.Command, viewModel.ShowExecutingFilesCommand));

		// Act
		sut.Click(sut.CenterOf(button));

		Dispatcher.UIThread.RunJobs();

		RightSideSheetContentKind content = viewModel.RightSideSheetContent;

		bool isFocusInside = sut
			.GetVisualDescendants()
			.OfType<SideSheet>()
			.Single()
			.IsKeyboardFocusWithin;

		sut.Close();

		// Assert
		content
			.Should()
			.Be(RightSideSheetContentKind.ExecutingFiles);

		isFocusInside
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFavoritesCommand" />: the item of the side menu switches to the favorites.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Favorites_Menu_Item_Switches_To_The_Favorites()
	{
		// Arrange
		FileDto favorite = ItemDtoFactory.CreateFileDto();

		favorite.IsFavorite = true;

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(favorite);

		EditorWindow sut = new(viewModel);

		sut.Show();

		// The menu is in the side drawer, which the user opens first.
		viewModel.IsLeftDrawerOpened = true;

		Dispatcher.UIThread.RunJobs();

		NavigationDrawer drawer = sut
			.GetLogicalDescendants()
			.OfType<NavigationDrawer>()
			.Single();

		MenuItem item = ((Panel)drawer.LeftDrawerContent!).Children
			.OfType<Menu>()
			.Single()
			.Items
			.OfType<MenuItem>()
			.Single(x => x.Command == viewModel.ShowFavoritesCommand);

		// Act
		item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

		sut.Close();

		// Assert
		mainWindowLauncher
			.Received(1)
			.SwitchToFavorites();
	}

	/// <summary>
	/// <see cref="ViewModelBase.ShowNotepadCommand" />: a click on the notepad button of the title bar opens the notepad
	/// for the window.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Notepad_Button_Shows_The_Notepad()
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

		EditorWindow sut = new(mock.Create<EditorViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = sut.CenterOf(sut.NotepadButton);

		// Act
		sut.Click(point);

		// Assert
		secondaryWindowLauncher
			.Received(1)
			.ShowNotepadWindow();
	}

	/// <summary>
	/// <see cref="EditorViewModel.CollapseAllFoldersCommand" />: every folder of the tree is shown collapsed, nested ones too,
	/// and nothing stays selected.
	/// </summary>
	[AvaloniaTest]
	public async Task CollapseAllFoldersCommand_Collapses_Every_Folder_And_Clears_The_Selection()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(outer);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TreeViewItem outerItem = sut.FindTreeItem(outer)!;

		outerItem.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);

		Dispatcher.UIThread.RunJobs();

		TreeViewItem innerItem = sut.FindTreeItem(inner)!;

		innerItem.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);

		Dispatcher.UIThread.RunJobs();

		sut.FindTreeItem(file)!.SetCurrentValue(TreeViewItem.IsSelectedProperty, true);

		Dispatcher.UIThread.RunJobs();

		TreeView tree = sut
			.GetVisualDescendants()
			.OfType<TreeView>()
			.Single();

		// The tree shows the folders expanded and the file selected first, so the collapse has something to undo.
		bool wasShown = outerItem.IsExpanded && innerItem.IsExpanded && tree.SelectedItem is not null;

		// Act
		await viewModel
			.CollapseAllFoldersCommand
			.ExecuteAsync(null);

		Dispatcher.UIThread.RunJobs();

		bool[] states = [outerItem.IsExpanded, innerItem.IsExpanded];

		object? selected = tree.SelectedItem;

		sut.Close();

		// Assert
		wasShown
			.Should()
			.BeTrue();

		states
			.Should()
			.OnlyContain(static x => !x);

		selected
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EditorWindow()" />: the separator and the seeding items at the end of the menu are shown only in a debug
	/// build.
	/// </summary>
	[AvaloniaTest]
	public void Constructor_Shows_The_Seeding_Items_Only_In_A_Debug_Build()
	{
		// Act
		EditorWindow sut = new();

		// Assert
		Control[] items =
		[
			sut.SeedingSeparator,
			sut.SampleSeeding,
			sut.LargeSampleSeeding,
			sut.ClipboardHistorySeeding
		];

		// The failure names the elements: in an optimized build the message can quote another assertion.
		items.Where(x => x.IsVisible != AppInfo.IsDebug).Select(x => x.Name)
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: the context menu of an object in the tree hands the object to its commands.
	/// </summary>
	[AvaloniaTest]
	public void ContextMenu_Of_An_Object_In_The_Tree_Passes_The_Object_To_Its_Commands([Values] bool isFolder)
	{
		// Arrange
		ExplorerItemDtoBase dto = isFolder ? ItemDtoFactory.CreateFolderDto() : ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		// The launcher gives every editor window the width of its navigation column.
		viewModel.SetDefaultNavigationColumnWidth(1000.0);

		viewModel.Hierarchy.Add(dto);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Grid row = sut.FindTreeRow(dto)!;

		// Act
		row.ContextFlyout!.ShowAt(row);

		Dispatcher.UIThread.RunJobs();

		object?[] parameters =
		[
			.. ((Control)((Flyout)row.ContextFlyout!).Content!)
				.GetLogicalDescendants()
				.OfType<Button>()
				.Where(static x => x.Command is not null)
				.Select(static x => x.CommandParameter)
		];

		row.ContextFlyout.Hide();

		sut.Close();

		// Assert
		parameters
			.Should()
			.NotBeEmpty()
			.And
			.OnlyContain(x => ReferenceEquals(x, dto));
	}

	/// <summary>
	/// <see cref="EditorViewModel.CopyContentByContextMenuCommand" />: the copy from the menu of a file in the tree
	/// highlights the file there.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentByContextMenuCommand_Highlights_The_File_In_The_Tree()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

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

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(file);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TreeViewItem item = sut.FindTreeItem(file)!;

		IBrush? background = item.Background;

		// Act
		await viewModel.CopyContentByContextMenuCommand.ExecuteAsync(file);

		IBrush? highlight = item.Background;

		sut.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
	}

	/// <summary>
	/// <see cref="EditorViewModel.CopyNameCommand" />: the copied name of an object highlights the object in the tree.
	/// </summary>
	[AvaloniaTest]
	public void CopyNameCommand_Highlights_The_Object_In_The_Tree()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(file);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TreeViewItem item = sut.FindTreeItem(file)!;

		IBrush? background = item.Background;

		// Act
		viewModel.CopyNameCommand.Execute(file);

		IBrush? highlight = item.Background;

		sut.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
	}

	/// <summary>
	/// <see cref="EditorViewModel.CopyNameCommand" />: the name of an object goes to the clipboard as an ordinary text.
	/// </summary>
	[AvaloniaTest]
	public void CopyNameCommand_Writes_The_Name_To_The_Clipboard()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IClipboardTextWriter clipboardWriter = Substitute.For<IClipboardTextWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(clipboardWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(file);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.CopyNameCommand.Execute(file);

		sut.Close();

		// Assert
		clipboardWriter
			.Received(1)
			.WriteTextAsync(file.Name, false);
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteCommand" />: the command of the toolbar deletes the object the user selects in the
	/// tree.
	/// </summary>
	[AvaloniaTest]
	public async Task DeleteCommand_Deletes_The_Object_Selected_In_The_Tree()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IDeletionScenario deletion = Substitute.For<IDeletionScenario>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesNoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(deletion);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		// The launcher gives every editor window the width of its navigation column.
		viewModel.SetDefaultNavigationColumnWidth(1000.0);

		viewModel.Hierarchy.Add(file);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		sut.Click(sut.CenterOf(sut.FindTreeRow(file)!));

		Dispatcher.UIThread.RunJobs();

		// Act
		await viewModel
			.DeleteCommand
			.ExecuteAsync(null);

		sut.Close();

		// Assert
		await deletion.Received(1).DeleteAsync(
			file,
			Arg.Any<Collection<ExplorerItemDtoBase>>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the rows of the copy history that the side sheet shows bind without
	/// an error while the editor closes.
	/// </summary>
	[AvaloniaTest]
	public void Dispose_Binds_The_Shown_Copy_History_Without_An_Error()
	{
		// Arrange
		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		RecordingBindingLogSink log = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([first.Id, second.Id]));

			session
				.Hierarchy
				.AddRange([first, second]);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		int shownFiles = sut
			.GetVisualDescendants()
			.OfType<ListBoxItem>()
			.Count(static x => x.DataContext is FileDto);

		ILogSink? previous = Logger.Sink;

		Logger.Sink = log;

		try
		{
			// Act
			viewModel.Dispose();

			Dispatcher.UIThread.RunJobs();
		}
		finally
		{
			Logger.Sink = previous;
		}

		sut.Close();

		// Assert
		shownFiles
			.Should()
			.BePositive();

		log.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: a double click on a file in the tree opens the context menu of the file.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_A_File_In_The_Tree_Opens_Its_Context_Menu()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		// The launcher gives every editor window the width of its navigation column.
		viewModel.SetDefaultNavigationColumnWidth(1000.0);

		viewModel.Hierarchy.Add(file);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Grid item = sut.FindTreeRow(file)!;

		Point point = sut.CenterOf(item);

		// Act
		sut.Click(point);

		sut.Click(point);

		bool isOpen = item.ContextFlyout is { IsOpen: true };

		item.ContextFlyout?.Hide();

		sut.Close();

		// Assert
		isOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewModelBase.CenterNotepadCommand" />: a double click on the notepad button of the title bar brings the
	/// notepad to the screen of the window.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Notepad_Button_Centers_The_Notepad()
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

		EditorWindow sut = new(mock.Create<EditorViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = sut.CenterOf(sut.NotepadButton);

		// Act
		sut.Click(point);

		sut.Click(point);

		// Assert
		secondaryWindowLauncher
			.Received(1)
			.CenterNotepadWindow();
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditingFiles" />: the tabs view of a loaded window opens the tabs the work session keeps.
	/// </summary>
	[AvaloniaTest]
	public void EditingFiles_Restores_The_Tabs_Of_The_Session_When_The_Window_Loads()
	{
		// Arrange
		// A kind with no editor keeps the content of the tab light.
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 2, kind: EntityKind.Folder)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new()
			{
				EditorTabs = new()
				{
					Files = files,
					PreviousFile = files[0],
					SelectedFile = files[1]
				}
			};

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		// Act
		sut.Show();

		Dispatcher.UIThread.RunJobs();

		sut.Close();

		// Assert
		viewModel.EditingFiles.Items
			.Should()
			.Equal(files);
	}

	/// <summary>
	/// <see cref="EditorWindow" />: Escape right after a click on the copy history button closes the side sheet the click
	/// opened.
	/// </summary>
	[AvaloniaTest]
	public void Escape_After_A_Click_On_The_Copy_History_Button_Closes_The_Side_Sheet()
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

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Button button = sut
			.GetVisualDescendants()
			.OfType<Button>()
			.Single(x => ReferenceEquals(x.Command, viewModel.ShowCopyHistoryCommand));

		sut.Click(sut.CenterOf(button));

		Dispatcher.UIThread.RunJobs();

		bool wasOpened = viewModel.IsRightSideSheetOpened;

		// Act
		sut.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		sut.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		bool isOpened = viewModel.IsRightSideSheetOpened;

		sut.Close();

		// Assert
		wasOpened
			.Should()
			.BeTrue();

		isOpened
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: Escape with the focus inside the side sheet closes the sheet.
	/// </summary>
	[AvaloniaTest]
	public void Escape_Inside_The_Side_Sheet_Closes_It([Values] bool isSearchBoxFocused)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([file.Id]));

			session
				.Hierarchy
				.Add(file);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		Visual[] list =
		[
			.. sut
				.GetVisualDescendants()
				.OfType<CopyHistoryView>()
				.Single()
				.GetVisualDescendants()
		];

		InputElement target = isSearchBoxFocused
			? list.OfType<TextBox>().First()
			: list.OfType<Button>().First(static x => x.DataContext is FileDto);

		target.Focus();

		Dispatcher.UIThread.RunJobs();

		bool wasFocusInside = sut
			.GetVisualDescendants()
			.OfType<SideSheet>()
			.Single()
			.IsKeyboardFocusWithin;

		// Act
		sut.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		sut.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		bool isOpened = viewModel.IsRightSideSheetOpened;

		sut.Close();

		// Assert
		wasFocusInside
			.Should()
			.BeTrue();

		isOpened
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: Escape with the focus outside the side sheet leaves the sheet open.
	/// </summary>
	[AvaloniaTest]
	public void Escape_Outside_The_Side_Sheet_Leaves_It_Open()
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

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		sut.NotepadButton.Focus();

		Dispatcher.UIThread.RunJobs();

		bool wasButtonFocused = sut.NotepadButton.IsKeyboardFocusWithin;

		// Act
		sut.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		sut.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		bool isOpened = viewModel.IsRightSideSheetOpened;

		sut.Close();

		// Assert
		wasButtonFocused
			.Should()
			.BeTrue();

		isOpened
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: Escape that a part of the side sheet takes for itself, as the open menu of a row does,
	/// leaves the sheet open.
	/// </summary>
	[AvaloniaTest]
	public void Escape_Taken_Inside_The_Side_Sheet_Leaves_It_Open()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([file.Id]));

			session
				.Hierarchy
				.Add(file);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		Button button = sut
			.GetVisualDescendants()
			.OfType<CopyHistoryView>()
			.Single()
			.GetVisualDescendants()
			.OfType<Button>()
			.First(static x => x.DataContext is FileDto);

		// The row takes the key, as the presenter of its open menu does.
		button.KeyDown += static (_, e) => e.Handled |= e.Key == Key.Escape;

		button.Focus();

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		sut.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		bool isOpened = viewModel.IsRightSideSheetOpened;

		sut.Close();

		// Assert
		isOpened
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: a folder the user expands in the tree is shown expanded again when the editor opens the
	/// next time, also in the read-only mode that does not save it.
	/// </summary>
	[AvaloniaTest]
	public void Expand_Of_A_Folder_In_The_Tree_Lasts_Until_The_Editor_Opens_Again([Values] bool isReadOnly)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		file.Parent = folder;

		folder.Children.Add(file);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel first = mock.Create<EditorViewModel>();

		first.IsReadOnly = isReadOnly;

		first.Hierarchy.Add(folder);

		EditorWindow firstWindow = new(first);

		firstWindow.Show();

		Dispatcher.UIThread.RunJobs();

		firstWindow.FindTreeItem(folder)!.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);

		Dispatcher.UIThread.RunJobs();

		// The switch to the favorites closes the window and drops its view model.
		firstWindow.Close();

		first.Dispose();

		using ILifetimeScope scope = mock.Container.BeginLifetimeScope();

		EditorWindow sut = new(scope.Resolve<EditorViewModel>());

		// Act
		sut.Show();

		Dispatcher.UIThread.RunJobs();

		bool isExpanded = sut.FindTreeItem(folder) is { IsExpanded: true };

		sut.Close();

		// Assert
		isExpanded
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: a folder the user expands in the tree is saved expanded.
	/// </summary>
	[AvaloniaTest]
	public async Task Expand_Of_A_Folder_In_The_Tree_Saves_It_Expanded()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		file.Parent = folder;

		folder.Children.Add(file);

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(folder);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TreeViewItem item = sut.FindTreeItem(folder)!;

		// Act
		item.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);

		sut.Close();

		// Assert
		await propertyWriter
			.Received(1)
			.UpdateIsExpandedAsync(folder.Id, true, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExpandAllFoldersCommand" />: every folder of the tree is shown expanded, nested ones too.
	/// </summary>
	[AvaloniaTest]
	public async Task ExpandAllFoldersCommand_Expands_Every_Folder_Of_The_Tree()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(outer);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		await viewModel
			.ExpandAllFoldersCommand
			.ExecuteAsync(null);

		Dispatcher.UIThread.RunJobs();

		bool[] states =
		[
			sut.FindTreeItem(outer) is { IsExpanded: true },
			sut.FindTreeItem(inner) is { IsExpanded: true }
		];

		bool isFileShown = sut.FindTreeItem(file) is { IsEffectivelyVisible: true };

		sut.Close();

		// Assert
		states
			.Should()
			.OnlyContain(static x => x);

		isFileShown
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewModelBase.IsClipboardHistoryEnabled" />: the clipboard history seeding item of the open menu is
	/// unavailable while the history is off, and only then has a tip that tells why.
	/// </summary>
	[AvaloniaTest]
	public void IsClipboardHistoryEnabled_Gives_The_Seeding_Item_A_Tip_Only_While_Off([Values] bool isEnabled)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.IsClipboardHistoryEnabled = isEnabled;

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.IsLeftDrawerOpened = true;

		Dispatcher.UIThread.RunJobs();

		bool isItemEnabled = sut.ClipboardHistorySeeding.IsEffectivelyEnabled;

		bool hasTip = ToolTip.GetTip(sut.ClipboardHistorySeeding) is not null;

		bool showsTipWhileDisabled = ToolTip.GetShowOnDisabled(sut.ClipboardHistorySeeding);

		sut.Close();

		// Assert
		isItemEnabled
			.Should()
			.Be(isEnabled);

		hasTip
			.Should()
			.Be(!isEnabled);

		showsTipWhileDisabled
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ExplorerNodeViewModelBase.IsSelected" />: a choice in the tree made in a shown window that has not loaded
	/// yet is not saved.
	/// </summary>
	[AvaloniaTest]
	public async Task IsSelected_Is_Not_Saved_Before_The_Window_Loads()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel
			.Hierarchy
			.Add(file);

		EditorWindow window = new(viewModel);

		// Shown, but not loaded until the dispatcher runs.
		window.Show();

		// Act
		viewModel.Nodes.Single().IsSelected = true;

		window.Close();

		// Assert
		await propertyWriter
			.DidNotReceive()
			.UpdateIsSelectedAsync(Arg.Any<ExplorerItemDtoBase>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ExplorerNodeViewModelBase.IsSelected" />: a choice in the tree made while the window is hidden is not
	/// saved.
	/// </summary>
	[AvaloniaTest]
	public async Task IsSelected_Is_Not_Saved_While_The_Window_Is_Hidden()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel
			.Hierarchy
			.Add(file);

		EditorWindow window = new(viewModel);

		window.Show();

		Dispatcher.UIThread.RunJobs();

		window.Hide();

		// Act
		viewModel.Nodes.Single().IsSelected = true;

		window.Close();

		// Assert
		await propertyWriter
			.DidNotReceive()
			.UpdateIsSelectedAsync(Arg.Any<ExplorerItemDtoBase>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ExplorerNodeViewModelBase.IsSelected" />: a choice in the tree is saved once the window is loaded and
	/// visible.
	/// </summary>
	[AvaloniaTest]
	public async Task IsSelected_Is_Saved_Once_The_Window_Is_Shown()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel
			.Hierarchy
			.Add(file);

		EditorWindow window = new(viewModel);

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.Nodes.Single().IsSelected = true;

		window.Close();

		// Assert
		await propertyWriter
			.Received(1)
			.UpdateIsSelectedAsync(file, true, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.NavigationColumnWidth" />: the width a dragged splitter gives the navigation column comes
	/// back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void NavigationColumnWidth_Follows_The_Navigation_Column()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Initialize(CreateWindowSettings(navigationColumnWidth: 150.0));

		EditorWindow sut = new(viewModel)
		{
			Width = 1000.0
		};

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		FindNavigationColumn(sut).Width = new GridLength(222.0);

		sut.Close();

		// Assert
		viewModel.NavigationColumnWidth
			.Should()
			.Be(222.0);
	}

	/// <summary>
	/// <see cref="EditorViewModel.NavigationColumnWidth" />: the width of the view model reaches the navigation column.
	/// </summary>
	[AvaloniaTest]
	public void NavigationColumnWidth_Reaches_The_Navigation_Column()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Initialize(CreateWindowSettings(navigationColumnWidth: 150.0));

		EditorWindow sut = new(viewModel)
		{
			Width = 1000.0
		};

		// Act
		sut.Show();

		Dispatcher.UIThread.RunJobs();

		GridLength width = FindNavigationColumn(sut).Width;

		sut.Close();

		// Assert
		width
			.Should()
			.Be(new GridLength(150.0));
	}

	/// <summary>
	/// <see cref="EditorViewModel.Receive(ShowInEditorMessage)" />: a request of an embedded editor to show its file selects
	/// the file in the tree.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_ShowInEditor_Selects_The_Object()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(file);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.Receive(new ShowInEditorMessage(file.Id));

		await Dispatcher.UIThread.RunJobsUntilAsync(() => viewModel.SelectedNode?.Model == file);

		sut.Close();

		// Assert
		viewModel.SelectedNode!.Model
			.Should()
			.BeSameAs(file);
	}

	/// <summary>
	/// <see cref="EditorViewModel.RightSideSheetContent" />: the rows of the copy history that leaves the side sheet bind
	/// without an error.
	/// </summary>
	[AvaloniaTest]
	[TestCase(RightSideSheetContentKind.ExecutingFiles)]
	[TestCase(RightSideSheetContentKind.None)]
	public void RightSideSheetContent_Change_Binds_The_Leaving_Copy_History_Without_An_Error(RightSideSheetContentKind next)
	{
		// Arrange
		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		RecordingBindingLogSink log = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([first.Id, second.Id]));

			session
				.Hierarchy
				.AddRange([first, second]);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		int shownFiles = sut
			.GetVisualDescendants()
			.OfType<ListBoxItem>()
			.Count(static x => x.DataContext is FileDto);

		ILogSink? previous = Logger.Sink;

		Logger.Sink = log;

		try
		{
			// Act
			viewModel.RightSideSheetContent = next;

			Dispatcher.UIThread.RunJobs();
		}
		finally
		{
			Logger.Sink = previous;
		}

		sut.Close();

		// Assert
		shownFiles
			.Should()
			.BePositive();

		log.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EditorViewModel.RightSideSheetContent" />: the open menu of a file of the copy history that leaves the
	/// side sheet binds without an error.
	/// </summary>
	[AvaloniaTest]
	public void RightSideSheetContent_Change_Binds_The_Open_Menu_Of_A_Leaving_File_Without_An_Error()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		RecordingBindingLogSink log = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([file.Id]));

			session
				.Hierarchy
				.Add(file);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		Grid row = sut
			.GetVisualDescendants()
			.OfType<CopyHistoryView>()
			.Single()
			.GetVisualDescendants()
			.OfType<Grid>()
			.First(static x => x.DataContext is FileDto && x.ContextFlyout is LazyMenuFlyout);

		row.ContextFlyout!.ShowAt(row);

		Dispatcher.UIThread.RunJobs();

		bool isMenuOpen = row.ContextFlyout!.IsOpen;

		ILogSink? previous = Logger.Sink;

		Logger.Sink = log;

		try
		{
			// Act
			viewModel.RightSideSheetContent = RightSideSheetContentKind.None;

			Dispatcher.UIThread.RunJobs();
		}
		finally
		{
			Logger.Sink = previous;
		}

		sut.Close();

		// Assert
		isMenuOpen
			.Should()
			.BeTrue();

		log.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: the rows of folders and files bind without an error, also while the tree expands them and
	/// takes them back as it scrolls.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Of_A_Long_Tree_Binds_Every_Row_Without_An_Error()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy =
		[
			.. Enumerable
				.Range(0, 50)
				.SelectMany(static _ =>
				{
					FolderDto folder = ItemDtoFactory.CreateFolderDto();

					foreach (FileDto child in ItemDtoFactory.CreateFileDtos(2))
					{
						child.Parent = folder;

						folder.Children.Add(child);
					}

					return new ExplorerItemDtoBase[] { folder, ItemDtoFactory.CreateFileDto() };
				})
		];

		RecordingBindingLogSink log = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		// The launcher gives every editor window the width of its navigation column.
		viewModel.SetDefaultNavigationColumnWidth(1000.0);

		viewModel
			.Hierarchy
			.AddRange(hierarchy);

		EditorWindow sut = new(viewModel);

		ILogSink? previous = Logger.Sink;

		Logger.Sink = log;

		try
		{
			// Act
			sut.Show();

			Dispatcher.UIThread.RunJobs();

			await viewModel
				.ExpandAllFoldersCommand
				.ExecuteAsync(null);

			Dispatcher.UIThread.RunJobs();

			ScrollViewer scroller = sut
				.GetVisualDescendants()
				.OfType<TreeView>()
				.Single()
				.FindDescendantOfType<ScrollViewer>()!;

			foreach (double share in (double[])[1.0, 0.5, 0.0])
			{
				scroller.Offset = new(0.0, scroller.Extent.Height * share);

				Dispatcher.UIThread.RunJobs();
			}

			sut.Close();
		}
		finally
		{
			Logger.Sink = previous;
		}

		// Assert
		log.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EditorWindow" />: the rows the tree takes back while it scrolls show the folders with their objects and
	/// the files with none.
	/// </summary>
	[AvaloniaTest]
	public void Scroll_Of_A_Long_Tree_Keeps_The_Objects_Of_Every_Folder()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy =
		[
			.. Enumerable
				.Range(0, 50)
				.SelectMany(static _ =>
				{
					FolderDto folder = ItemDtoFactory.CreateFolderDto();

					foreach (FileDto child in ItemDtoFactory.CreateFileDtos(2))
					{
						child.Parent = folder;

						folder.Children.Add(child);
					}

					return new ExplorerItemDtoBase[] { folder, ItemDtoFactory.CreateFileDto() };
				})
		];

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		// The launcher gives every editor window the width of its navigation column.
		viewModel.SetDefaultNavigationColumnWidth(1000.0);

		viewModel
			.Hierarchy
			.AddRange(hierarchy);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		ScrollViewer scroller = sut
			.GetVisualDescendants()
			.OfType<TreeView>()
			.Single()
			.FindDescendantOfType<ScrollViewer>()!;

		// Act
		foreach (double share in (double[])[1.0, 0.5, 0.0])
		{
			scroller.Offset = new(0.0, scroller.Extent.Height * share);

			Dispatcher.UIThread.RunJobs();
		}

		(ExplorerItemDtoBase Dto, int Count)[] shown =
		[
			.. sut
				.GetVisualDescendants()
				.OfType<Grid>()
				.Where(static x => x.IsEffectivelyVisible
					&& x.DataContext is ExplorerItemDtoBase
					&& x.ContextFlyout is LazyMenuFlyout)
				.Select(static x => ((ExplorerItemDtoBase)x.DataContext!, x.FindAncestorOfType<TreeViewItem>()!.ItemCount))
		];

		sut.Close();

		// Assert
		shown
			.Should()
			.Contain(static x => x.Dto is FolderDto)
			.And
			.Contain(static x => x.Dto is FileDto)
			.And
			.OnlyContain(static x => x.Count == (x.Dto is FolderDto ? ((FolderDto)x.Dto).Children.Count : 0));
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowCopyHistoryCommand" />: the side sheet shows the list of the copy history that the
	/// window made.
	/// </summary>
	[AvaloniaTest]
	public void ShowCopyHistoryCommand_Shows_The_List_Of_The_Window()
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

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		object? shown = sut
			.GetVisualDescendants()
			.OfType<CopyHistoryView>()
			.Single()
			.DataContext;

		CopyHistoryViewModel? list = viewModel.CopyHistoryList;

		sut.Close();

		// Assert
		shown
			.Should()
			.NotBeNull()
			.And
			.BeSameAs(list);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowExecutingFilesCommand" />: the side sheet shows the list of the executing files and
	/// not the copy history.
	/// </summary>
	[AvaloniaTest]
	public void ShowExecutingFilesCommand_Shows_The_List_Of_The_Executing_Files()
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

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel
			.ShowExecutingFilesCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		Control[] shown =
		[
			.. sut
				.GetVisualDescendants()
				.OfType<Control>()
				.Where(x => x is ExecutingFilesView or CopyHistoryView)
		];

		sut.Close();

		// Assert
		shown
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ExecutingFilesView>();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFavorites" />: the rows of the copy history that the side sheet shows bind without an
	/// error while the editor switches to the favorites.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Binds_The_Shown_Copy_History_Without_An_Error()
	{
		// Arrange
		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		RecordingBindingLogSink log = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([first.Id, second.Id]));

			session
				.Hierarchy
				.AddRange([first, second]);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		int shownFiles = sut
			.GetVisualDescendants()
			.OfType<ListBoxItem>()
			.Count(static x => x.DataContext is FileDto);

		ILogSink? previous = Logger.Sink;

		Logger.Sink = log;

		try
		{
			// Act
			viewModel.ShowFavorites();

			Dispatcher.UIThread.RunJobs();
		}
		finally
		{
			Logger.Sink = previous;
		}

		sut.Close();

		// Assert
		shownFiles
			.Should()
			.BePositive();

		log.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowInListCommand" />: a file chosen in the list of the executing files is selected in the
	/// tree.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowInListCommand_Selects_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(file);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.ShowInListCommand.Execute(file.Id);

		await Dispatcher.UIThread.RunJobsUntilAsync(() => viewModel.SelectedNode?.Model == file);

		sut.Close();

		// Assert
		viewModel.SelectedNode!.Model
			.Should()
			.BeSameAs(file);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowObject" />: one show builds the row of an object inside collapsed folders far down the
	/// tree, brings it into view, selects it and highlights it.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowObject_Brings_An_Object_Inside_Collapsed_Folders_Far_Down_Into_View()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		// The launcher gives every editor window the width of its navigation column.
		viewModel.SetDefaultNavigationColumnWidth(1000.0);

		// The tree builds no rows of the top level far from its view, nor the rows inside them.
		viewModel
			.Hierarchy
			.AddRange(ItemDtoFactory.CreateFileDtos(100));

		viewModel.Hierarchy.Add(outer);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.ShowObject(file.Id);

		await Dispatcher.UIThread.RunJobsUntilAsync(() => IsHighlighted(sut, file, inner));

		bool[] states =
		[
			sut.FindTreeItem(file) is { IsSelected: true },
			IsInView(sut, file),
			IsHighlighted(sut, file, inner)
		];

		sut.Close();

		// Assert
		states
			.Should()
			.OnlyContain(static x => x);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowObject" />: an object far down the tree is brought into view.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowObject_Brings_The_Object_Into_View()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(100)];

		FileDto shown = files[90];

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		// The launcher gives every editor window the width of its navigation column.
		viewModel.SetDefaultNavigationColumnWidth(1000.0);

		viewModel
			.Hierarchy
			.AddRange(files);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.ShowObject(shown.Id);

		await Dispatcher.UIThread.RunJobsUntilAsync(() => viewModel.SelectedNode?.Model == shown);

		Dispatcher.UIThread.RunJobs();

		bool isShown = sut.FindTreeItem(shown) is { IsEffectivelyVisible: true };

		sut.Close();

		// Assert
		isShown
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowObject" />: the collapsed folders of an object are shown expanded and saved so.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowObject_Expands_The_Collapsed_Folders_Of_The_Object()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(outer);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.ShowObject(file.Id);

		Dispatcher.UIThread.RunJobs();

		bool[] states =
		[
			sut.FindTreeItem(outer) is { IsExpanded: true },
			sut.FindTreeItem(inner) is { IsExpanded: true },
			sut.FindTreeItem(file) is { IsEffectivelyVisible: true }
		];

		sut.Close();

		// Assert
		states
			.Should()
			.OnlyContain(static x => x);

		await propertyWriter
			.Received(1)
			.UpdateIsExpandedAsync(outer.Id, true, Arg.Any<CancellationToken>());

		await propertyWriter
			.Received(1)
			.UpdateIsExpandedAsync(inner.Id, true, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowObject" />: an object inside nested folders is highlighted in its own place
	/// of the tree.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowObject_Highlights_An_Object_Inside_Nested_Folders()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			// Expanded before the tree shows them, so the item of the file is there to compare.
			session.SetExpanded(outer.Id, true);

			session.SetExpanded(inner.Id, true);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(outer);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TreeViewItem item = sut.FindTreeItem(file)!;

		IBrush? background = item.Background;

		// Act
		viewModel.ShowObject(file.Id);

		await Dispatcher.UIThread.RunJobsUntilAsync(() => !ReferenceEquals(item.Background, background));

		IBrush? highlight = item.Background;

		sut.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowObject" />: the shown object is highlighted in the tree.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowObject_Highlights_The_Object_In_The_Tree()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(file);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TreeViewItem item = sut.FindTreeItem(file)!;

		IBrush? background = item.Background;

		// Act
		viewModel.ShowObject(file.Id);

		await Dispatcher.UIThread.RunJobsUntilAsync(() => !ReferenceEquals(item.Background, background));

		IBrush? highlight = item.Background;

		sut.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowObject" />: a file inside collapsed folders stays selected, as its row is built
	/// before the selection.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowObject_Selects_A_File_Inside_Collapsed_Folders()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(outer);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.ShowObject(file.Id);

		// The highlight comes after the selection, so a selection the tree drops is gone by then.
		await Dispatcher.UIThread.RunJobsUntilAsync(() => IsHighlighted(sut, file, inner));

		bool[] states =
		[
			viewModel.SelectedNode?.Model == file,
			sut.FindTreeItem(file) is { IsSelected: true }
		];

		sut.Close();

		// Assert
		states
			.Should()
			.OnlyContain(static x => x);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the settings of an editor window with a navigation column of a width.
	/// </summary>
	private static EditorWindowSettings CreateWindowSettings(double navigationColumnWidth)
	{
		return new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = navigationColumnWidth,
			Size = new(1000, 600),
			WindowState = WindowShowState.Normal,
			X = 0,
			Y = 0
		};
	}

	/// <summary>
	/// Returns the column of the tree in a window.
	/// </summary>
	private static ColumnDefinition FindNavigationColumn(EditorWindow window)
	{
		return ((Grid)window
			.GetVisualDescendants()
			.OfType<TreeView>()
			.First()
			.Parent!)
			.ColumnDefinitions[0];
	}

	/// <summary>
	/// <c>True</c> while the row of an object is highlighted: its background differs from that of the row of another
	/// object.
	/// </summary>
	private static bool IsHighlighted(EditorWindow window, ExplorerItemDtoBase dto, ExplorerItemDtoBase other)
	{
		return window.FindTreeItem(dto) is { } item
			&& window.FindTreeItem(other) is { } otherItem
			&& !ReferenceEquals(item.Background, otherItem.Background);
	}

	/// <summary>
	/// <c>True</c> while the row of an object lies wholly in the visible part of the tree.
	/// </summary>
	private static bool IsInView(EditorWindow window, ExplorerItemDtoBase dto)
	{
		return window.FindTreeRow(dto) is { } row
			&& row.FindAncestorOfType<TreeView>() is { } tree
			&& row.TranslatePoint(default, tree) is { } point
			&& point.Y >= 0.0
			&& point.Y + row.Bounds.Height <= tree.Bounds.Height;
	}
	#endregion
}
