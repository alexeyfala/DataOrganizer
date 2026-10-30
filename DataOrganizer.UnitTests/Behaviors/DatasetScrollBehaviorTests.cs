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
using DataOrganizer.Behaviors;
using DataOrganizer.UnitTests.Factories;
using DatasetRecords.Dto;
using DatasetRecords.Models;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace DataOrganizer.UnitTests.Behaviors;

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
	/// Time longer than the pause after which a scroll is reported, and than the rest of a move.
	/// </summary>
	private static readonly TimeSpan PastThePause = TimeSpan.FromSeconds(1.0);

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
	public void Click_On_A_Record_Leaves_The_Scroll_Back_Going()
	{
		// Arrange
		const double offset = (TopRecordIndex * RecordHeight) + WithinRecordOffset;

		FakeTimeProvider time = new();

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
			TimeProvider = time,
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
		time.RunJobsUntil(() => scrollViewer.Offset.Y > scrollViewer.Viewport.Height);

		Point point = scrollViewer.TranslatePoint(new Rect(scrollViewer.Viewport).Center, window)!.Value;

		// Act
		window.Click(point);

		time.RunJobsUntil(() => Math.Abs(scrollViewer.Offset.Y - offset) < 0.5);

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
	public void Click_On_The_Track_Stops_The_Scroll_Back()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time,
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
		time.RunJobsUntil(() => scrollViewer.Offset.Y > scrollViewer.Viewport.Height);

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
		time.RunJobsFor(PastThePause);

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(place.Y, 0.5);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.Detach" />: a behavior taken off the list no longer reports its scroll.
	/// </summary>
	[AvaloniaTest]
	public void Detach_Stops_Reporting_The_Scroll()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		sut.IsEnabled = true;

		time.RunJobsFor(PastThePause);

		// Act
		Interaction
			.GetBehaviors(scrollViewer)
			.Remove(sut);

		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		time.RunJobsFor(PastThePause);

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
	public void Drag_Of_The_Thumb_Stops_The_Scroll_Back()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time,
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
		time.RunJobsUntil(() => scrollViewer.Offset.Y > scrollViewer.Viewport.Height);

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
		time.RunJobsFor(PastThePause);

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
	public void IsEnabled_Brings_Back_The_View_State()
	{
		// Arrange
		const double offset = (TopRecordIndex * RecordHeight) + WithinRecordOffset;

		FakeTimeProvider time = new();

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
			TimeProvider = time,
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

		time.RunJobsUntil(() => Math.Abs(scrollViewer.Offset.Y - offset) < 0.5);

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(offset, 0.5);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: enabled as the records come, before a layout pass has realized
	/// them, the behavior brings <see cref="DatasetScrollBehavior.ViewState" /> back as soon as they are laid out.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Brings_Back_The_View_State_Once_The_Records_Are_Laid_Out()
	{
		// Arrange
		const double offset = (TopRecordIndex * RecordHeight) + WithinRecordOffset;

		FakeTimeProvider time = new();

		ItemsRepeater recordsHost = new()
		{
			ItemTemplate = RecordTemplate
		};

		ScrollViewer scrollViewer = new()
		{
			Content = recordsHost
		};

		DatasetScrollBehavior sut = new()
		{
			TimeProvider = time,
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
		recordsHost.ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount);

		sut.IsEnabled = true;

		time.RunJobsFor(PastThePause);

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
	public void IsEnabled_Ends_The_Move_When_The_Record_Is_Gone()
	{
		// Arrange
		const int keptCount = 20;

		ObservableCollection<DatasetRecordBase> records = [.. DatasetRecordFactory.CreateValueRecords(RecordCount)];

		FakeTimeProvider time = new();

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
			TimeProvider = time,
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

		time.RunJobsFor(PastThePause);

		scrollViewer.Offset = new Vector(0.0, RecordHeight + WithinRecordOffset);

		time.RunJobsUntil(() => sut.ViewState is { TopRecordIndex: not TopRecordIndex });

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
	public void IsEnabled_Follows_The_Scroll_After_A_Record_Before_The_List()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time,
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

		time.RunJobsFor(PastThePause);

		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		time.RunJobsUntil(() => sut.ViewState is { TopRecordIndex: >= 0 });

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
	public void IsEnabled_Follows_The_Scroll_After_A_Record_Near_The_End()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time,
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

		time.RunJobsUntil(() => Math.Abs(scrollViewer.Offset.Y - End()) < 0.5);

		// The move ends one step after the list reaches its end.
		time.RunJobsFor(PastThePause);

		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		time.RunJobsUntil(() => sut.ViewState is { TopRecordIndex: TopRecordIndex });

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
	public void IsEnabled_Is_Off_Until_It_Is_Set()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		time.RunJobsFor(PastThePause);

		// Act
		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		time.RunJobsFor(PastThePause);

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
	public void IsEnabled_Leaves_The_List_For_A_Record_Beyond_It()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time,
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

		time.RunJobsFor(PastThePause);

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
	public void IsEnabled_Leaves_The_View_State_Of_A_Record_Near_The_End()
	{
		// Arrange
		DatasetEditorState state = new()
		{
			TopRecordIndex = RecordCount - 2,
			WithinRecordOffset = 0.0
		};

		FakeTimeProvider time = new();

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
			TimeProvider = time,
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

		time.RunJobsUntil(() => Math.Abs(scrollViewer.Offset.Y - End()) < 0.5);

		time.RunJobsFor(PastThePause);

		// Assert
		sut.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: turned off, the behavior no longer reports the scroll.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Off_Stops_Reporting_The_Scroll()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		sut.IsEnabled = true;

		time.RunJobsFor(PastThePause);

		// Act
		sut.IsEnabled = false;

		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		time.RunJobsFor(PastThePause);

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
	public void IsEnabled_Puts_A_List_Without_A_View_State_At_Its_Top()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		scrollViewer.Offset = new Vector(0.0, LayoutShift);

		// Act
		sut.IsEnabled = true;

		time.RunJobsFor(PastThePause);

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.Be(0.0);

		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a layout pass that realizes no records does not end the wait for
	/// them, so the behavior brings <see cref="DatasetScrollBehavior.ViewState" /> back once the records are laid out.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Waits_Until_A_Layout_Pass_Realizes_The_Records()
	{
		// Arrange
		const double offset = (TopRecordIndex * RecordHeight) + WithinRecordOffset;

		FakeTimeProvider time = new();

		ItemsRepeater recordsHost = new()
		{
			// A hidden list lays out no records.
			IsVisible = false,
			ItemsSource = DatasetRecordFactory.CreateValueRecords(RecordCount),
			ItemTemplate = RecordTemplate
		};

		ScrollViewer scrollViewer = new()
		{
			Content = recordsHost
		};

		DatasetScrollBehavior sut = new()
		{
			TimeProvider = time,
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

		// A pass of the layout of the window that the hidden list takes no part in.
		window.Width += 10.0;

		Dispatcher.UIThread.RunJobs();

		// Act
		recordsHost.IsVisible = true;

		time.RunJobsFor(PastThePause);

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(offset, 0.5);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: enabled before the list is shown, the behavior brings
	/// <see cref="DatasetScrollBehavior.ViewState" /> back once it is.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Waits_Until_The_List_Is_Shown()
	{
		// Arrange
		const double offset = (TopRecordIndex * RecordHeight) + WithinRecordOffset;

		FakeTimeProvider time = new();

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
			TimeProvider = time,
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

		time.RunJobsUntil(() => Math.Abs(scrollViewer.Offset.Y - offset) < 0.5);

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
	public void Page_Key_Stops_The_Scroll_Back([Values(PhysicalKey.PageUp, PhysicalKey.PageDown)] PhysicalKey key)
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time,
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
		time.RunJobsUntil(() => scrollViewer.Offset.Y > scrollViewer.Viewport.Height);

		// Act
		window.KeyPressQwerty(key, RawInputModifiers.None);

		Vector place = scrollViewer.Offset;

		// Longer than the rest of the scroll back would take.
		time.RunJobsFor(PastThePause);

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
	public void Scroll_Reports_The_Record_At_The_Top()
	{
		// Arrange
		FakeTimeProvider time = new();

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
			TimeProvider = time
		};

		Interaction
			.GetBehaviors(scrollViewer)
			.Add(sut);

		Show(scrollViewer);

		sut.IsEnabled = true;

		time.RunJobsFor(PastThePause);

		// Act
		scrollViewer.Offset = new Vector(0.0, (TopRecordIndex * RecordHeight) + WithinRecordOffset);

		time.RunJobsUntil(() => sut.ViewState is not null);

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
