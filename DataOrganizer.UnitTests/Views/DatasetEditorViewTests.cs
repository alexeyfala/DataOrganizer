using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Dto.Dataset;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Models.Dataset;
using DataOrganizer.Services.Documents;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using Material.Icons;
using Material.Icons.Avalonia;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetEditorView)}"" type")]
internal class DatasetEditorViewTests
{
	#region Data
	/// <summary>
	/// Name of the template part of an expander that shows its header and expands it.
	/// </summary>
	private const string HeaderPartName = "PART_ToggleButton";

	/// <summary>
	/// Number of the records that the tests load.
	/// </summary>
	private const int RecordCount = 50;

	/// <summary>
	/// Name of the list of the records.
	/// </summary>
	private const string RecordsHostName = "RecordsHost";

	/// <summary>
	/// Name of the scroll viewer of the records.
	/// </summary>
	private const string RecordsScrollerName = "RecordsScroller";

	/// <summary>
	/// Index of the record that a scroll brings to the top, one that the list shows from the start.
	/// </summary>
	private const int ScrolledRecordIndex = 5;

	/// <summary>
	/// Index of the stored record at the top.
	/// </summary>
	private const int StoredRecordIndex = 30;

	/// <summary>
	/// Distance from the top edge of the record at the top to the top of the list.
	/// </summary>
	private const double WithinRecordOffset = 7.0;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DatasetEditorView" />: a click on a scroll button of the toolbar brings the list to that edge.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_Brings_The_List_To_The_Edge([Values] bool toEnd)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Show(sut);

		// The list starts in the middle, so either edge is a move away.
		await WaitUntilScrolledBackAsync(recordsHost, recordsScroller);

		MaterialIconKind icon = toEnd ? MaterialIconKind.ArrowCollapseDown : MaterialIconKind.ArrowCollapseUp;

		Button button = sut
			.GetLogicalDescendants()
			.OfType<Button>()
			.Single(x => x.Content is MaterialIcon { Kind: var kind } && kind == icon);

		double Edge()
		{
			return toEnd ? recordsScroller.Extent.Height - recordsScroller.Viewport.Height : 0.0;
		}

		// Act
		Click(button);

		await RunJobsUntilAsync(() => Math.Abs(recordsScroller.Offset.Y - Edge()) < 0.5);

		// Assert
		recordsScroller.Offset.Y
			.Should()
			.BeApproximately(Edge(), 0.5);
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a click on a scroll button of the toolbar while the list scrolls back to the
	/// stored record brings the list to that edge, and the list stays there.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_During_The_Scroll_Back_Brings_The_List_To_The_Edge([Values] bool toEnd)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		MaterialIconKind icon = toEnd ? MaterialIconKind.ArrowCollapseDown : MaterialIconKind.ArrowCollapseUp;

		Button button = sut
			.GetLogicalDescendants()
			.OfType<Button>()
			.Single(x => x.Content is MaterialIcon { Kind: var kind } && kind == icon);

		double Edge()
		{
			return toEnd ? recordsScroller.Extent.Height - recordsScroller.Viewport.Height : 0.0;
		}

		Show(sut);

		// Past its first screen the list is on its way back to the stored record.
		await RunJobsUntilAsync(() => recordsScroller.Offset.Y > recordsScroller.Viewport.Height);

		// Act
		Click(button);

		await RunJobsUntilAsync(() => Math.Abs(recordsScroller.Offset.Y - Edge()) < 0.5);

		// Longer than the rest of the scroll back would take.
		await OutlastThePauseAsync();

		// Assert
		recordsScroller.Offset.Y
			.Should()
			.BeApproximately(Edge(), 0.5);
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a group expanded or collapsed by a click on its chevron saves the records as a
	/// change of the view.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_The_Chevron_Of_A_Group_Saves_A_View_Change()
	{
		// Arrange
		IFileContentsWriter fileContentsWriter = Substitute.For<IFileContentsWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			DatasetRecordBase[] records =
			[
				new RecordsGroup
				{
					Children = [.. DatasetRecordFactory.CreateValueRecords(2)],
					Name = "Group"
				}
			];

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(records),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			fileContentsWriter
				.WriteAsync(default, default, default!, default, default)
				.ReturnsForAnyArgs(new FileContentsWriting
				{
					Status = FileContentsWriteStatus.Written
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder.RegisterInstance(fileContentsWriter);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		Show(sut);

		await RunJobsUntilAsync(() => viewModel.IsInitialized && FindGroupExpander(sut, "Group") is not null);

		ToggleButton chevron = FindChevron(FindGroupExpander(sut, "Group")!);

		// Act
		Click(chevron);

		await RunJobsAsync(repeats: 10);

		// Assert
		await fileContentsWriter
			.Received(1)
			.WriteAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid?>(),
				Arg.Any<byte[]>(),
				FileContentsChange.View,
				Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a click on the chevron of a group inside another one saves the records once, not
	/// once more for the outer group.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_The_Chevron_Of_A_Nested_Group_Saves_Once()
	{
		// Arrange
		IFileContentsWriter fileContentsWriter = Substitute.For<IFileContentsWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			DatasetRecordBase[] records =
			[
				new RecordsGroup
				{
					Children =
					[
						new RecordsGroup
						{
							Children = [.. DatasetRecordFactory.CreateValueRecords(2)],
							Name = "Inner"
						}
					],
					Name = "Outer"
				}
			];

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(records),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			fileContentsWriter
				.WriteAsync(default, default, default!, default, default)
				.ReturnsForAnyArgs(new FileContentsWriting
				{
					Status = FileContentsWriteStatus.Written
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder.RegisterInstance(fileContentsWriter);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		Show(sut);

		await RunJobsUntilAsync(() => viewModel.IsInitialized && FindGroupExpander(sut, "Inner") is not null);

		ToggleButton chevron = FindChevron(FindGroupExpander(sut, "Inner")!);

		// Act
		Click(chevron);

		await RunJobsAsync(repeats: 10);

		// Assert
		await fileContentsWriter
			.Received(1)
			.WriteAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid?>(),
				Arg.Any<byte[]>(),
				FileContentsChange.View,
				Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: while one scroll button of the toolbar moves the list, a click on the other one
	/// does not start a move against it.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_Runs_One_Scroll_Button_At_A_Time([Values] bool toEndFirst)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Show(sut);

		await WaitUntilScrolledBackAsync(recordsHost, recordsScroller);

		Button[] buttons = [.. sut.GetLogicalDescendants().OfType<Button>()];

		Button toTop = buttons.Single(static x => x.Content is MaterialIcon { Kind: MaterialIconKind.ArrowCollapseUp });

		Button toEnd = buttons.Single(static x => x.Content is MaterialIcon { Kind: MaterialIconKind.ArrowCollapseDown });

		double Edge()
		{
			return toEndFirst ? recordsScroller.Extent.Height - recordsScroller.Viewport.Height : 0.0;
		}

		// Act
		Click(toEndFirst ? toEnd : toTop);

		Click(toEndFirst ? toTop : toEnd);

		await RunJobsUntilAsync(() => Math.Abs(recordsScroller.Offset.Y - Edge()) < 0.5);

		// Assert
		recordsScroller.Offset.Y
			.Should()
			.BeApproximately(Edge(), 0.5);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsEditingEnabled" />: the edits are open while the list scrolls back to the
	/// stored record.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEditingEnabled_Does_Not_Wait_For_The_List_To_Scroll_Back()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		bool? isEditingEnabled = null;

		// Past its first screen the list is on its way back to the stored record.
		using IDisposable firstMove = recordsScroller
			.GetObservable(ScrollViewer.OffsetProperty)
			.Where(x => x.Y > recordsScroller.Viewport.Height)
			.Take(1)
			.Subscribe(_ => isEditingEnabled = viewModel.IsEditingEnabled);

		// Act
		Show(sut);

		await WaitUntilScrolledBackAsync(recordsHost, recordsScroller);

		// Assert
		isEditingEnabled
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="RecordsGroup.IsExpanded" />: a group expanded or collapsed by code, with the pointer elsewhere, saves
	/// nothing through its expander.
	/// </summary>
	[AvaloniaTest]
	public async Task IsExpanded_Changed_By_Code_Saves_Nothing()
	{
		// Arrange
		IFileContentsWriter fileContentsWriter = Substitute.For<IFileContentsWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			DatasetRecordBase[] records =
			[
				new RecordsGroup
				{
					Children = [.. DatasetRecordFactory.CreateValueRecords(2)],
					Name = "Group"
				}
			];

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(records),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder.RegisterInstance(fileContentsWriter);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		Window window = Show(sut);

		await RunJobsUntilAsync(() => viewModel.IsInitialized && FindGroupExpander(sut, "Group") is not null);

		// The pointer rests in a corner of the window, away from the group.
		window.MouseMove(new Point(window.Width - 1.0, window.Height - 1.0));

		RecordsGroup group = (RecordsGroup)viewModel.Records[0];

		// Act
		group.IsExpanded = false;

		await RunJobsAsync(repeats: 10);

		// Assert
		await fileContentsWriter
			.DidNotReceiveWithAnyArgs()
			.WriteAsync(default, default, default!, default, default);
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: shown again with fewer records, the list does not save the scroll its layout makes
	/// to fit them.
	/// </summary>
	[AvaloniaTest]
	public async Task Loaded_Again_Does_Not_Save_The_Scroll_Of_The_Layout()
	{
		// Arrange
		const int keptCount = 10;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The dataset has no stored place yet.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Window window = Show(sut);

		await WaitUntilFollowedAsync(viewModel, recordsHost);

		ScrollToRecord(recordsHost, recordsScroller, ScrolledRecordIndex);

		await RunJobsUntilAsync(() => reported is not null);

		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		// The records left no longer reach the place, so the layout of the list scrolls it up as it is shown again.
		while (viewModel.Records.Count > keptCount)
		{
			viewModel
				.Records
				.RemoveAt(viewModel.Records.Count - 1);
		}

		// Act
		window.Content = sut;

		await OutlastThePauseAsync();

		// Assert
		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: shown again, the list keeps the place it was left at instead of going back to
	/// the stored record.
	/// </summary>
	[AvaloniaTest]
	public async Task Loaded_Again_Keeps_The_Place_Of_The_List()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Window window = Show(sut);

		await WaitUntilScrolledBackAsync(recordsHost, recordsScroller);

		// Hidden before the pause after the scroll ends, so the place shown differs from the stored one.
		ScrollToRecord(recordsHost, recordsScroller, StoredRecordIndex + 1);

		Dispatcher.UIThread.RunJobs();

		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		// Act
		window.Content = sut;

		await OutlastThePauseAsync();

		// Assert
		FindRecordTop(recordsHost, recordsScroller, StoredRecordIndex + 1)
			.Should()
			.BeApproximately(-WithinRecordOffset, 0.5);
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: shown, the list brings back to the top the record of the state it is given,
	/// and the stored state is left unread.
	/// </summary>
	[AvaloniaTest]
	public async Task Loaded_Brings_Back_The_Given_State()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			initialEditorState: new SystemTextJsonSerializer().Serialize(new DatasetEditorState
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			})));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		// Act
		Show(sut);

		await WaitUntilScrolledBackAsync(recordsHost, recordsScroller);

		// Assert
		FindRecordTop(recordsHost, recordsScroller, StoredRecordIndex)
			.Should()
			.BeApproximately(-WithinRecordOffset, 0.5);

		await dbAccess
			.DidNotReceive()
			.GetFileEditorStateAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: shown, the list brings back to the top the stored record, scrolled as far into
	/// it as it was.
	/// </summary>
	[AvaloniaTest]
	public async Task Loaded_Brings_Back_The_Stored_Record()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		// Act
		Show(sut);

		await WaitUntilScrolledBackAsync(recordsHost, recordsScroller);

		// Assert
		FindRecordTop(recordsHost, recordsScroller, StoredRecordIndex)
			.Should()
			.BeApproximately(-WithinRecordOffset, 0.5);
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: the place the list is brought back to is not saved again, since nobody moved it.
	/// </summary>
	[AvaloniaTest]
	public async Task Loaded_Does_Not_Save_The_Restored_Place()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		// Act
		Show(sut);

		await WaitUntilScrolledBackAsync(recordsHost, recordsScroller);

		await OutlastThePauseAsync();

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a stored state that cannot be read leaves the editor open.
	/// </summary>
	[AvaloniaTest]
	public async Task Loaded_Keeps_The_Editor_Open_For_A_Damaged_Stored_State()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The stored state breaks off after its first character.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns("{");

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		// Act
		Show(sut);

		await RunJobsUntilAsync(() => viewModel.IsInitialized);

		await OutlastThePauseAsync();

		// Assert
		viewModel.IsEditingEnabled
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a shown group, its header and its chevron are named for UI Automation as the field
	/// of its header: by the name of the group, or by the neutral word while the dataset is encrypted.
	/// </summary>
	[AvaloniaTest]
	public async Task Loaded_Names_A_Group_As_Its_Header_Field([Values] bool isEncrypted)
	{
		// Arrange
		const string groupName = "Group";

		Guid? keeperId = isEncrypted
			? Guid.NewGuid()
			: null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			DatasetRecordBase[] records =
			[
				new RecordsGroup
				{
					Children = [.. DatasetRecordFactory.CreateValueRecords(2)],
					Name = groupName
				}
			];

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(records),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The contents of an encrypted dataset come back from the session key of its keeper as they were stored.
			if (keeperId is { } id)
			{
				IContentCipher contentCipher = Substitute.For<IContentCipher>();

				contentCipher
					.TryDecrypt(id, ContentIdentity.Contents, Arg.Any<byte[]>())
					.Returns(x => x.ArgAt<byte[]>(2));

				builder.RegisterInstance(contentCipher);
			}

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: keeperId));

		DatasetEditorView sut = new(viewModel);

		// Act
		Show(sut);

		await RunJobsUntilAsync(() => viewModel.IsInitialized && FindGroupExpander(sut, groupName) is not null);

		// Assert
		Expander expander = FindGroupExpander(sut, groupName)!;

		DatasetFieldView field = expander
			.GetVisualDescendants()
			.OfType<DatasetFieldView>()
			.First(static x => x.Text == groupName);

		ToggleButton header = expander
			.GetVisualDescendants()
			.OfType<ToggleButton>()
			.Single(static x => x.Name == HeaderPartName);

		string? name = GetAutomationName(expander);

		name
			.Should()
			.NotBeNullOrEmpty()
			.And
			.Be(GetAutomationName(field));

		GetAutomationName(header)
			.Should()
			.Be(name);

		GetAutomationName(FindChevron(expander))
			.Should()
			.Be(name);
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a stored record beyond the end of the list leaves the list on its first records.
	/// </summary>
	[AvaloniaTest]
	public async Task Loaded_Shows_The_First_Records_For_A_Stored_Record_Beyond_The_List()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = RecordCount,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		// Act
		Show(sut);

		await RunJobsUntilAsync(() => viewModel.IsInitialized);

		await OutlastThePauseAsync();

		// Assert
		recordsScroller.Offset.Y
			.Should()
			.BeLessThan(recordsScroller.Viewport.Height);
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a change of the size of the list that keeps its place does not save the place.
	/// </summary>
	[AvaloniaTest]
	public async Task Resize_Does_Not_Save_The_Place()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The dataset has no stored place yet.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		Window window = Show(sut);

		await WaitUntilFollowedAsync(viewModel, recordsHost);

		// Act
		window.Height = 250.0;

		await OutlastThePauseAsync();

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: once the editor is closed, a scroll of the list is not saved.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Does_Not_Save_The_Place_After_The_Editor_Is_Closed()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The dataset has no stored place yet.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Show(sut);

		await WaitUntilFollowedAsync(viewModel, recordsHost);

		viewModel.Dispose();

		// Act
		ScrollToRecord(recordsHost, recordsScroller, ScrolledRecordIndex);

		await OutlastThePauseAsync();

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a scroll is not saved when the list is hidden before the pause after it ends, as
	/// on a quick switch of the tabs.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Is_Not_Saved_When_The_List_Is_Hidden_During_The_Pause()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The dataset has no stored place yet.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Window window = Show(sut);

		await WaitUntilFollowedAsync(viewModel, recordsHost);

		ScrollToRecord(recordsHost, recordsScroller, ScrolledRecordIndex);

		Dispatcher.UIThread.RunJobs();

		// Act
		window.Content = null;

		await OutlastThePauseAsync();

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: in the read-only mode a scroll of the list reaches the file but is not saved.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Reports_The_Place_Without_Saving_It_In_The_ReadOnly_Mode()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The dataset has no stored place yet.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		viewModel.IsReadOnly = true;

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Show(sut);

		await WaitUntilFollowedAsync(viewModel, recordsHost);

		// Act
		ScrollToRecord(recordsHost, recordsScroller, ScrolledRecordIndex);

		await RunJobsUntilAsync(() => reported is not null);

		// Assert
		DatasetEditorState state = new SystemTextJsonSerializer().Deserialize<DatasetEditorState>(reported!);

		state.TopRecordIndex
			.Should()
			.Be(ScrolledRecordIndex);

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: shown again after it was hidden, the list still saves the place a scroll brings
	/// it to.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Saves_The_Place_After_The_List_Is_Shown_Again()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The dataset has no stored place yet.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileId: Guid.NewGuid(),
			setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Window window = Show(sut);

		await WaitUntilFollowedAsync(viewModel, recordsHost);

		// A switch of the tabs takes the list off the window and brings it back.
		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		window.Content = sut;

		await RunJobsUntilAsync(() => recordsHost.TryGetElement(ScrolledRecordIndex) is not null);

		// Act
		ScrollToRecord(recordsHost, recordsScroller, ScrolledRecordIndex);

		await RunJobsUntilAsync(() => reported is not null);

		// Assert
		DatasetEditorState state = new SystemTextJsonSerializer().Deserialize<DatasetEditorState>(reported!);

		state.TopRecordIndex
			.Should()
			.Be(ScrolledRecordIndex);

		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(viewModel.FileId, reported!, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a dataset that opens without records still saves the place a scroll brings its
	/// list to once records are added.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Saves_The_Place_Of_A_Dataset_That_Opened_Empty()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileId: Guid.NewGuid(),
			setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Show(sut);

		await RunJobsUntilAsync(() => viewModel.IsInitialized);

		viewModel
			.Records
			.AddRange(DatasetRecordFactory.CreateValueRecords(RecordCount));

		await RunJobsUntilAsync(() => recordsHost.TryGetElement(ScrolledRecordIndex) is not null);

		// Act
		ScrollToRecord(recordsHost, recordsScroller, ScrolledRecordIndex);

		await RunJobsUntilAsync(() => reported is not null);

		// Assert
		DatasetEditorState state = new SystemTextJsonSerializer().Deserialize<DatasetEditorState>(reported!);

		state.TopRecordIndex
			.Should()
			.Be(ScrolledRecordIndex);

		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(viewModel.FileId, reported!, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: after a scroll of the list and a pause, the record at the top is saved, scrolled
	/// as far into it as it is, and the file gets the same state.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Saves_The_Record_At_The_Top()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The dataset has no stored place yet.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileId: Guid.NewGuid(),
			setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Show(sut);

		await WaitUntilFollowedAsync(viewModel, recordsHost);

		// Act
		ScrollToRecord(recordsHost, recordsScroller, ScrolledRecordIndex);

		await RunJobsUntilAsync(() => reported is not null);

		// Assert
		DatasetEditorState state = new SystemTextJsonSerializer().Deserialize<DatasetEditorState>(reported!);

		state.TopRecordIndex
			.Should()
			.Be(ScrolledRecordIndex);

		state.WithinRecordOffset
			.Should()
			.BeApproximately(WithinRecordOffset, 0.5);

		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(viewModel.FileId, reported!, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a scroll that comes back to the place saved last before the pause ends does not
	/// save anything.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Saves_The_Same_Place_Once()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The dataset has no stored place yet.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Show(sut);

		await WaitUntilFollowedAsync(viewModel, recordsHost);

		ScrollToRecord(recordsHost, recordsScroller, ScrolledRecordIndex);

		Vector place = recordsScroller.Offset;

		await RunJobsUntilAsync(() => reported is not null);

		// Act
		recordsScroller.Offset = place + new Vector(0.0, 1.0);

		// One run of the jobs lays the move out, far within the pause on any machine, so only the place where the scroll
		// ends counts.
		Dispatcher.UIThread.RunJobs();

		recordsScroller.Offset = place;

		await OutlastThePauseAsync();

		// Assert
		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a turn of the mouse wheel while the list scrolls back to the stored record stops
	/// the list where the wheel leaves it, and that place is saved after the pause.
	/// </summary>
	[AvaloniaTest]
	public async Task Wheel_During_The_Scroll_Back_Stops_The_List_And_Saves_Its_Place()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileId: Guid.NewGuid(),
			setEditorStateCallback: x => reported = x));

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Window window = Show(sut);

		// Past its first screen the list is on its way back to the stored record.
		await RunJobsUntilAsync(() => recordsScroller.Offset.Y > recordsScroller.Viewport.Height);

		Point center = recordsScroller.TranslatePoint(new Rect(recordsScroller.Bounds.Size).Center, window)!.Value;

		// Act
		window.MouseWheel(center, new Vector(0.0, 1.0));

		Vector place = recordsScroller.Offset;

		await RunJobsUntilAsync(() => reported is not null);

		// Assert
		recordsScroller.Offset.Y
			.Should()
			.BeApproximately(place.Y, 0.5);

		DatasetEditorState state = new SystemTextJsonSerializer().Deserialize<DatasetEditorState>(reported!);

		FindRecordTop(recordsHost, recordsScroller, state.TopRecordIndex)
			.Should()
			.BeApproximately(-state.WithinRecordOffset, 0.5);

		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(viewModel.FileId, reported!, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorView" />: a turn of the mouse wheel while a scroll button of the toolbar moves the list stops
	/// the list where the wheel leaves it.
	/// </summary>
	[AvaloniaTest]
	public async Task Wheel_During_The_Scroll_To_The_Edge_Stops_The_List([Values] bool toEnd)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = StoredRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		DatasetEditorView sut = new(viewModel);

		ItemsRepeater recordsHost = sut.GetControl<ItemsRepeater>(RecordsHostName);

		ScrollViewer recordsScroller = sut.GetControl<ScrollViewer>(RecordsScrollerName);

		Window window = Show(sut);

		// The list starts in the middle, so either edge is a move away.
		await WaitUntilScrolledBackAsync(recordsHost, recordsScroller);

		MaterialIconKind icon = toEnd ? MaterialIconKind.ArrowCollapseDown : MaterialIconKind.ArrowCollapseUp;

		Button button = sut
			.GetLogicalDescendants()
			.OfType<Button>()
			.Single(x => x.Content is MaterialIcon { Kind: var kind } && kind == icon);

		Point center = recordsScroller.TranslatePoint(new Rect(recordsScroller.Bounds.Size).Center, window)!.Value;

		// The move takes its first step at once and the rest later.
		Click(button);

		// Act
		window.MouseWheel(center, new Vector(0.0, 1.0));

		Vector place = recordsScroller.Offset;

		// Longer than the rest of the move would take.
		await OutlastThePauseAsync();

		// Assert
		recordsScroller.Offset.Y
			.Should()
			.BeApproximately(place.Y, 0.5);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Clicks <paramref name="button" /> with the left mouse button.
	/// </summary>
	private static void Click(Button button)
	{
		TopLevel topLevel = TopLevel.GetTopLevel(button)!;

		Point center = button.TranslatePoint(new Rect(button.Bounds.Size).Center, topLevel)!.Value;

		topLevel.MouseDown(center, MouseButton.Left);

		topLevel.MouseUp(center, MouseButton.Left);
	}

	/// <summary>
	/// Returns the chevron in the header of the expander of a group, which expands and collapses the group.
	/// </summary>
	private static ToggleButton FindChevron(Expander expander)
	{
		return expander
			.GetVisualDescendants()
			.OfType<ToggleButton>()
			.First(static x => x.Content is MaterialIcon);
	}

	/// <summary>
	/// Returns the expander of the group with a name; <c>null</c> while the view does not show it.
	/// </summary>
	private static Expander? FindGroupExpander(DatasetEditorView view, string name)
	{
		return view
			.GetVisualDescendants()
			.OfType<Expander>()
			.FirstOrDefault(x => x.DataContext is RecordsGroup group && group.Name == name);
	}

	/// <summary>
	/// Distance from the top of <paramref name="recordsScroller" /> to the top edge of the record at
	/// <paramref name="index" />; <c>null</c> while the list does not show the record.
	/// </summary>
	private static double? FindRecordTop(ItemsRepeater recordsHost, ScrollViewer recordsScroller, int index)
	{
		return recordsHost
			.TryGetElement(index)?
			.TranslatePoint(default, recordsScroller)?
			.Y;
	}

	/// <summary>
	/// Returns the name of <paramref name="control" /> for UI Automation.
	/// </summary>
	private static string? GetAutomationName(Control control)
	{
		return ControlAutomationPeer
			.CreatePeerForElement(control)
			.GetName();
	}

	/// <summary>
	/// Runs the jobs of the UI thread for longer than the pause after which a scroll is saved.
	/// </summary>
	private static Task OutlastThePauseAsync() => RunJobsAsync(repeats: 100);

	/// <summary>
	/// Runs the jobs of the UI thread <paramref name="repeats" /> times, with a short wait between the runs.
	/// </summary>
	private static async Task RunJobsAsync(int repeats)
	{
		Func<bool> never = static () =>
		{
			Dispatcher.UIThread.RunJobs();

			return false;
		};

		await never.WaitAsync(millisecondsDelay: 10, maxRepeats: repeats);
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

		await isMet.WaitAsync(millisecondsDelay: 10, maxRepeats: 500);
	}

	/// <summary>
	/// Scrolls the list from where it is, so that the record at <paramref name="index" /> comes to the top,
	/// <see cref="WithinRecordOffset" /> into it.
	/// </summary>
	private static void ScrollToRecord(ItemsRepeater recordsHost, ScrollViewer recordsScroller, int index)
	{
		// The bounds of a record follow the shown part of the list, so the move starts from where the record is shown.
		double top = FindRecordTop(recordsHost, recordsScroller, index) ?? throw new InvalidOperationException();

		recordsScroller.Offset += new Vector(0.0, top + WithinRecordOffset);
	}

	/// <summary>
	/// Shows <paramref name="view" /> in a window too short to hold all the records.
	/// </summary>
	private static Window Show(DatasetEditorView view)
	{
		Window window = new()
		{
			Content = view,
			Height = 300.0,
			Width = 400.0
		};

		window.Show();

		return window;
	}

	/// <summary>
	/// Runs the jobs of the UI thread until the list of a dataset without a stored place follows its scroll.
	/// </summary>
	private static async Task WaitUntilFollowedAsync(DatasetEditorViewModel viewModel, ItemsRepeater recordsHost)
	{
		await RunJobsUntilAsync(() => viewModel.IsInitialized && recordsHost.TryGetElement(ScrolledRecordIndex) is not null);

		// The list follows its scroll once it finds its records laid out, which it checks at a pace of its own.
		await RunJobsAsync(repeats: 60);
	}

	/// <summary>
	/// Runs the jobs of the UI thread until the list has brought the stored record back to the top and stopped there.
	/// </summary>
	private static async Task WaitUntilScrolledBackAsync(ItemsRepeater recordsHost, ScrollViewer recordsScroller)
	{
		await RunJobsUntilAsync(() => FindRecordTop(recordsHost, recordsScroller, StoredRecordIndex) is { } top
			&& Math.Abs(top + WithinRecordOffset) < 0.5);

		// The scroll back takes one more step to find that it has arrived, and a move before that step is undone.
		await RunJobsAsync(repeats: 10);
	}
	#endregion
}
