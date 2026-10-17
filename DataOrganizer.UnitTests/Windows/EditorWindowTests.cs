using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Views;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using Entities.Enums;
using NSubstitute;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(EditorWindow)}"" type")]
internal class EditorWindowTests
{
	#region Methods
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

		Point point = Center(sut, sut.NotepadButton);

		// Act
		Click(sut, point);

		// Assert
		secondaryWindowLauncher
			.Received(1)
			.ShowNotepadWindow(sut);
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

		TreeViewItem item = FindTreeItem(sut, file);

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

		TreeViewItem item = FindTreeItem(sut, file);

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

		Grid item = sut
			.GetVisualDescendants()
			.OfType<Grid>()
			.First(x => x.DataContext == file && Interaction.GetBehaviors(x).OfType<LazyContextFlyoutBehavior>().Any());

		Point point = Center(sut, item);

		// Act
		Click(sut, point);

		Click(sut, point);

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

		Point point = Center(sut, sut.NotepadButton);

		// Act
		Click(sut, point);

		Click(sut, point);

		// Assert
		secondaryWindowLauncher
			.Received(1)
			.CenterNotepadWindow(sut);
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

		await RunJobsUntilAsync(() => viewModel.SelectedObject == file);

		sut.Close();

		// Assert
		viewModel.SelectedObject
			.Should()
			.BeSameAs(file);
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
	/// <see cref="EditorViewModel.ShowInEditorAsync" />: an object far down the tree is brought into view.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowInEditorAsync_Brings_The_Object_Into_View()
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
		_ = viewModel.ShowInEditorAsync(shown.Id, sut);

		await RunJobsUntilAsync(() => viewModel.SelectedObject == shown);

		Dispatcher.UIThread.RunJobs();

		bool isShown = sut
			.GetVisualDescendants()
			.OfType<TreeViewItem>()
			.Any(x => x.DataContext == shown && x.IsEffectivelyVisible);

		sut.Close();

		// Assert
		isShown
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowInEditorAsync" />: an object inside nested folders is highlighted in its own place
	/// of the tree.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowInEditorAsync_Highlights_An_Object_Inside_Nested_Folders()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		// Expanded before the tree shows them, so the item of the file is there to compare.
		outer.IsExpanded = true;

		inner.IsExpanded = true;

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel viewModel = mock.Create<EditorViewModel>();

		viewModel.Hierarchy.Add(outer);

		EditorWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TreeViewItem item = FindTreeItem(sut, file);

		IBrush? background = item.Background;

		// Act
		_ = viewModel.ShowInEditorAsync(file.Id, sut);

		await RunJobsUntilAsync(() => !ReferenceEquals(item.Background, background));

		IBrush? highlight = item.Background;

		sut.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowInEditorAsync" />: the shown object is highlighted in the tree.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowInEditorAsync_Highlights_The_Object_In_The_Tree()
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

		TreeViewItem item = FindTreeItem(sut, file);

		IBrush? background = item.Background;

		// Act
		_ = viewModel.ShowInEditorAsync(file.Id, sut);

		await RunJobsUntilAsync(() => !ReferenceEquals(item.Background, background));

		IBrush? highlight = item.Background;

		sut.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
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

		await RunJobsUntilAsync(() => viewModel.SelectedObject == file);

		sut.Close();

		// Assert
		viewModel.SelectedObject
			.Should()
			.BeSameAs(file);
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
	/// Returns the point in the middle of the control.
	/// </summary>
	private static Point Center(Visual root, Visual target)
	{
		return target.TranslatePoint(
			new(
				target.Bounds.Width / 2.0,
				target.Bounds.Height / 2.0),
			root) ?? default;
	}

	/// <summary>
	/// Presses and releases the left button of the mouse at a point of a window.
	/// </summary>
	private static void Click(Window window, Point point)
	{
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);
	}

	/// <summary>
	/// Returns the item of the tree of a window that shows an object.
	/// </summary>
	private static TreeViewItem FindTreeItem(EditorWindow window, ExplorerItemDtoBase dto)
	{
		return window
			.GetVisualDescendants()
			.OfType<TreeViewItem>()
			.First(x => x.DataContext == dto);
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
	/// Runs the jobs of the UI thread until <paramref name="condition" /> holds or the wait runs out.
	/// </summary>

	private static async Task RunJobsUntilAsync(Func<bool> condition)
	{
		Func<bool> isMet = () =>
		{
			Dispatcher.UIThread.RunJobs();

			return condition();
		};

		await isMet.WaitAsync(millisecondsDelay: 10, maxRepeats: 300);
	}
	#endregion
}
