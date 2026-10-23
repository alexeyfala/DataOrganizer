using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Scrolling;
using DataOrganizer.Dto.Dataset;
using DataOrganizer.Models.Dataset;
using DataOrganizer.UnitTests.Factories;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Behaviors.Scrolling;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetScrollBehavior)}"" type")]
internal class DatasetScrollBehaviorTests
{
	#region Data
	/// <summary>
	/// Shift of the list off its top that the first layout of the records makes in the app.
	/// </summary>
	private const double LayoutShift = 40.0;

	/// <summary>
	/// Number of the records in the list.
	/// </summary>
	private const int RecordCount = 50;

	/// <summary>
	/// Height of a record.
	/// </summary>
	private const double RecordHeight = 40.0;

	/// <summary>
	/// Index of the record at the top.
	/// </summary>
	private const int TopRecordIndex = 30;

	/// <summary>
	/// Distance from the top edge of the record at the top to the top of the list.
	/// </summary>
	private const double WithinRecordOffset = 10.0;

	/// <summary>
	/// Template that shows every record at <see cref="RecordHeight" />, with a background the pointer can hit.
	/// </summary>
	private static readonly FuncDataTemplate<DatasetRecordBase> RecordTemplate = new((_, _) => new Border
	{
		Background = Brushes.Transparent,
		Height = RecordHeight
	});
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DatasetScrollBehavior" />: a click on a record while the list scrolls back to
	/// <see cref="DatasetScrollBehavior.ViewState" /> leaves the list on its way, since a click is not a scroll.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_A_Record_Leaves_The_Scroll_Back_Going()
	{
		// Arrange
		const double offset = (TopRecordIndex * RecordHeight) + WithinRecordOffset;

		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Window window = Show(scrollViewer);

		sut.IsEnabled = true;

		// Past its first screen the list is on its way back to the stored record.
		await RunJobsUntilAsync(() => scrollViewer.Offset.Y > scrollViewer.Viewport.Height);

		Point point = scrollViewer.TranslatePoint(new Rect(scrollViewer.Viewport).Center, window)!.Value;

		// Act
		window.Click(point);

		await RunJobsUntilAsync(() => Math.Abs(scrollViewer.Offset.Y - offset) < 0.5);

		// Assert
		window.InputHitTest(point)
			.Should()
			.BeOfType<Border>();

		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(offset, 0.5);
	}

	/// <summary>
	/// <see cref="DatasetScrollBehavior" />: a click on the track of the scroll bar while the list scrolls back to
	/// <see cref="DatasetScrollBehavior.ViewState" /> stops the list where the click leaves it.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_The_Track_Stops_The_Scroll_Back()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Window window = Show(scrollViewer);

		sut.IsEnabled = true;

		// Past its first screen the list is on its way back to the stored record.
		await RunJobsUntilAsync(() => scrollViewer.Offset.Y > scrollViewer.Viewport.Height);

		// The part of the track below the thumb.
		Button track = scrollViewer
			.GetVisualDescendants()
			.OfType<Track>()
			.Single(static x => x.Orientation == Orientation.Vertical)
			.IncreaseButton!;

		Point point = window.CenterOf(track);

		// Act
		window.Click(point);

		Vector place = scrollViewer.Offset;

		// Longer than the rest of the scroll back would take.
		await OutlastThePauseAsync();

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(place.Y, 0.5);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.Detach" />: a behavior taken off the list no longer reports its scroll.
	/// </summary>
	[AvaloniaTest]
	public async Task Detach_Stops_Reporting_The_Scroll()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new();

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		sut.IsEnabled = true;

		await RunJobsAsync(repeats: 10);

		// Act
		Interaction
			.GetBehaviors(scrollViewer)
			.Remove(sut);

		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		await OutlastThePauseAsync();

		// Assert
		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DatasetScrollBehavior" />: a drag of the thumb of the scroll bar while the list scrolls back to
	/// <see cref="DatasetScrollBehavior.ViewState" /> stops the list where the drag leaves it.
	/// </summary>
	[AvaloniaTest]
	public async Task Drag_Of_The_Thumb_Stops_The_Scroll_Back()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Window window = Show(scrollViewer);

		sut.IsEnabled = true;

		// Past its first screen the list is on its way back to the stored record.
		await RunJobsUntilAsync(() => scrollViewer.Offset.Y > scrollViewer.Viewport.Height);

		Thumb thumb = scrollViewer
			.GetVisualDescendants()
			.OfType<Track>()
			.Single(static x => x.Orientation == Orientation.Vertical)
			.Thumb!;

		Point from = window.CenterOf(thumb);

		Point to = from + new Point(0.0, 20.0);

		// Act
		window.MouseDown(from, MouseButton.Left);

		window.MouseMove(to, RawInputModifiers.LeftMouseButton);

		window.MouseUp(to, MouseButton.Left);

		Vector place = scrollViewer.Offset;

		// Longer than the rest of the scroll back would take.
		await OutlastThePauseAsync();

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(place.Y, 0.5);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: once enabled, the behavior brings the record of
	/// <see cref="DatasetScrollBehavior.ViewState" /> back to the top, scrolled as far into it as it was.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Brings_Back_The_View_State()
	{
		// Arrange
		const double offset = (TopRecordIndex * RecordHeight) + WithinRecordOffset;

		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		// Act
		sut.IsEnabled = true;

		await RunJobsUntilAsync(() => Math.Abs(scrollViewer.Offset.Y - offset) < 0.5);

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(offset, 0.5);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: records that go while the list moves back end the move, and the
	/// behavior still follows the scroll afterwards.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Ends_The_Move_When_The_Record_Is_Gone()
	{
		// Arrange
		const int keptCount = 20;

		ObservableCollection<DatasetRecordBase> records = [.. DatasetRecordFactory.CreateValueRecords(RecordCount)];

		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = records,
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		// Past its first screen the list is on its way back to the stored record, which then goes.
		using IDisposable deletion = scrollViewer
			.GetObservable(ScrollViewer.OffsetProperty)
			.Where(x => x.Y > scrollViewer.Viewport.Height)
			.Take(1)
			.Subscribe(_ =>
			{
				while (records.Count > keptCount)
				{
					records.RemoveAt(records.Count - 1);
				}
			});

		// Act
		sut.IsEnabled = true;

		await OutlastThePauseAsync();

		scrollViewer.Offset = new Vector(0.0, RecordHeight + WithinRecordOffset);

		await RunJobsUntilAsync(() => sut.ViewState is { TopRecordIndex: not TopRecordIndex });

		// Assert
		sut.ViewState
			.Should()
			.Be(new DatasetEditorState
			{
				TopRecordIndex = 1,
				WithinRecordOffset = WithinRecordOffset
			});
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a record before the start of the list is not looked for, and the
	/// behavior follows the scroll.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Follows_The_Scroll_After_A_Record_Before_The_List()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = -1,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		// Act
		sut.IsEnabled = true;

		await OutlastThePauseAsync();

		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		await RunJobsUntilAsync(() => sut.ViewState is { TopRecordIndex: >= 0 });

		// Assert
		sut.ViewState
			.Should()
			.Be(new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			});
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a record too near the end to come to the top brings the list to its
	/// end, and the behavior follows the scroll afterwards.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Follows_The_Scroll_After_A_Record_Near_The_End()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = RecordCount - 2,
				WithinRecordOffset = 0.0
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		double End()
		{
			return scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
		}

		// Act
		sut.IsEnabled = true;

		await RunJobsUntilAsync(() => Math.Abs(scrollViewer.Offset.Y - End()) < 0.5);

		// The move ends one step after the list reaches its end.
		await RunJobsAsync(repeats: 10);

		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		await RunJobsUntilAsync(() => sut.ViewState is { TopRecordIndex: TopRecordIndex });

		// Assert
		sut.ViewState
			.Should()
			.Be(new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			});
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: until it is enabled, the behavior does not report the scroll.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Is_Off_Until_It_Is_Set()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new();

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		await RunJobsAsync(repeats: 10);

		// Act
		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		await OutlastThePauseAsync();

		// Assert
		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a record beyond the end of the list leaves the list at its very top,
	/// though the first layout of the records has moved it off.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Leaves_The_List_For_A_Record_Beyond_It()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = RecordCount,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		scrollViewer.Offset = new Vector(0.0, LayoutShift);

		// Act
		sut.IsEnabled = true;

		await OutlastThePauseAsync();

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.Be(0.0);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a record too near the end to come to the top brings the list to its
	/// end, and <see cref="DatasetScrollBehavior.ViewState" /> stays as it was, since nobody scrolled the list.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Leaves_The_View_State_Of_A_Record_Near_The_End()
	{
		// Arrange
		DatasetEditorState state = new()
		{
			TopRecordIndex = RecordCount - 2,
			WithinRecordOffset = 0.0
		};

		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = state
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		double End()
		{
			return scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
		}

		// Act
		sut.IsEnabled = true;

		await RunJobsUntilAsync(() => Math.Abs(scrollViewer.Offset.Y - End()) < 0.5);

		await OutlastThePauseAsync();

		// Assert
		sut.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: turned off, the behavior no longer reports the scroll.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Off_Stops_Reporting_The_Scroll()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new();

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		sut.IsEnabled = true;

		await RunJobsAsync(repeats: 10);

		// Act
		sut.IsEnabled = false;

		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		await OutlastThePauseAsync();

		// Assert
		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a list without <see cref="DatasetScrollBehavior.ViewState" /> starts
	/// at its very top, though the first layout of the records has moved it off, and no place is reported for it.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Puts_A_List_Without_A_View_State_At_Its_Top()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new();

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		scrollViewer.Offset = new Vector(0.0, LayoutShift);

		// Act
		sut.IsEnabled = true;

		await OutlastThePauseAsync();

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.Be(0.0);

		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: enabled before the list is shown, the behavior brings
	/// <see cref="DatasetScrollBehavior.ViewState" /> back once it is.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEnabled_Waits_Until_The_List_Is_Shown()
	{
		// Arrange
		const double offset = (TopRecordIndex * RecordHeight) + WithinRecordOffset;

		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		sut.IsEnabled = true;

		// Act
		Show(scrollViewer);

		await RunJobsUntilAsync(() => Math.Abs(scrollViewer.Offset.Y - offset) < 0.5);

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(offset, 0.5);
	}

	/// <summary>
	/// <see cref="DatasetScrollBehavior" />: a page key while the list scrolls back to
	/// <see cref="DatasetScrollBehavior.ViewState" /> stops the list where the key leaves it.
	/// </summary>
	[AvaloniaTest]
	public async Task Page_Key_Stops_The_Scroll_Back([Values(PhysicalKey.PageUp, PhysicalKey.PageDown)] PhysicalKey key)
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			},
			// The keys reach the list through the control that has the focus.
			Focusable = true
		};

		DatasetScrollBehavior sut = new()
		{
			ViewState = new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			}
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Window window = Show(scrollViewer);

		scrollViewer.Focus();

		sut.IsEnabled = true;

		// Past its first screen the list is on its way back to the stored record.
		await RunJobsUntilAsync(() => scrollViewer.Offset.Y > scrollViewer.Viewport.Height);

		// Act
		window.KeyPressQwerty(key, RawInputModifiers.None);

		Vector place = scrollViewer.Offset;

		// Longer than the rest of the scroll back would take.
		await OutlastThePauseAsync();

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(place.Y, 0.5);
	}

	/// <summary>
	/// <see cref="DatasetScrollBehavior" />: after a scroll and a pause, the record at the top is reported, scrolled as far
	/// into it as it is.
	/// </summary>
	[AvaloniaTest]
	public async Task Scroll_Reports_The_Record_At_The_Top()
	{
		// Arrange
		ScrollViewer scrollViewer = new()
		{
			Content = new ItemsRepeater
			{
				ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
				ItemTemplate = RecordTemplate
			}
		};

		DatasetScrollBehavior sut = new();

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		sut.IsEnabled = true;

		await RunJobsAsync(repeats: 10);

		// Act
		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		await RunJobsUntilAsync(() => sut.ViewState is not null);

		// Assert
		sut.ViewState
			.Should()
			.Be(new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			});
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs the jobs of the UI thread for longer than the pause after which a scroll is reported.
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
	/// Shows <paramref name="scrollViewer" /> in a window too short to hold all the records, and lets it load.
	/// </summary>
	private static Window Show(ScrollViewer scrollViewer)
	{
		Window window = new()
		{
			Content = scrollViewer,
			Height = 200.0,
			Width = 300.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
