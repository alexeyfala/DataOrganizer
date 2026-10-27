using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Behaviors.DragAndDrop;
using DataOrganizer.Interfaces;
using DataOrganizer.UnitTests.Factories;
using DatasetRecords.Messages;
using DatasetRecords.Models;
using Shared.Common;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Behaviors.DragAndDrop;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetRecordDragBehavior)}"" type")]
internal class DatasetRecordDragBehaviorTests
{
	#region Data
	/// <summary>
	/// Height of the dragged element and of the drop target.
	/// </summary>
	private const double ElementHeight = 40.0;

	/// <summary>
	/// Width of the dragged element and of the drop target.
	/// </summary>
	private const double ElementWidth = 100.0;

	/// <summary>
	/// A move far enough to start a drag at the default threshold.
	/// </summary>
	private static readonly Vector Far = new(10.0, 0.0);

	/// <summary>
	/// A point over the drop target, which lies below the dragged element.
	/// </summary>
	private static readonly Point OverTarget = new(50.0, ElementHeight + 20.0);

	/// <summary>
	/// A point over the dragged element where the press happens.
	/// </summary>
	private static readonly Point Start = new(10.0, 10.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DatasetRecordDragBehavior.DragThreshold" />: a move shorter than the threshold in both directions starts
	/// no drag.
	/// </summary>
	[AvaloniaTest]
	public void DragThreshold_Keeps_A_Shorter_Move_From_Dragging(
		[Values(4.0, 10.0)] double threshold,
		[Values] bool isVertical)
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			DragThreshold = threshold,
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		Point end = Start + (isVertical
			? new Vector(0.0, threshold - 1.0)
			: new Vector(threshold - 1.0, 0.0));

		// Act
		Drag(window, Start, end);

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior.DragThreshold" />: a move as long as the threshold in either direction starts
	/// the drag.
	/// </summary>
	[AvaloniaTest]
	public void DragThreshold_Starts_The_Drag_At_Its_Distance(
		[Values(4.0, 10.0)] double threshold,
		[Values] bool isVertical)
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			DragThreshold = threshold,
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		Point end = Start + (isVertical
			? new Vector(0.0, threshold)
			: new Vector(threshold, 0.0));

		// Act
		Drag(window, Start, end);

		// Assert
		drags
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a behavior turned off after the press starts no drag on the move.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Off_Keeps_The_Move_From_Dragging()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		window.MouseDown(Start, MouseButton.Left);

		sut.IsEnabled = false;

		// Act
		window.MouseMove(Start + Far, RawInputModifiers.LeftMouseButton);

		window.MouseUp(Start + Far, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a press on a behavior turned off starts no drag, even when the
	/// behavior is turned on before the move.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Off_Keeps_The_Press_From_Dragging()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			IsEnabled = false,
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		window.MouseDown(Start, MouseButton.Left);

		sut.IsEnabled = true;

		// Act
		window.MouseMove(Start + Far, RawInputModifiers.LeftMouseButton);

		window.MouseUp(Start + Far, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: the drag carries the record and its text in one item and offers a copy
	/// and a move.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Drag_Carries_The_Record_And_Its_Text_In_One_Item()
	{
		// Arrange
		ValueRecord record = new();

		string text = RandomString.Create(10);

		int itemCount = 0;

		DragDropEffects offered = DragDropEffects.None;

		DatasetRecordBase? droppedRecord = null;

		string? droppedText = null;

		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		Border target = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		DragDrop.SetAllowDrop(target, true);

		target.AddHandler(DragDrop.DropEvent, (_, e) =>
		{
			itemCount = e.DataTransfer.Items.Count;

			offered = e.DragEffects;

			droppedRecord = e.DataTransfer.TryGetValue(DatasetRecordDragBehavior.RecordFormat);

			droppedText = e.DataTransfer.TryGetText();
		});

		StackPanel panel = new()
		{
			Children =
			{
				element,
				target
			},
			HorizontalAlignment = HorizontalAlignment.Left,
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Top
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = record,
			Text = text
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(panel);

		// Act
		Drag(window, Start, OverTarget);

		// Assert
		itemCount
			.Should()
			.Be(1);

		offered
			.Should()
			.Be(DragDropEffects.Copy | DragDropEffects.Move);

		droppedRecord
			.Should()
			.BeSameAs(record);

		droppedText
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: the end of a drag is announced once.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Drag_Ends_With_A_Message()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord(),
			Text = RandomString.Create(10)
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		// Act
		Drag(window, Start, Start + Far);

		// Assert
		drags
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: without a record and with a blank text there is nothing to drag.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Drag_Needs_A_Record_Or_A_Text()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = null,
			Text = " "
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		// Act
		Drag(window, Start, Start + Far);

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: a drop that copies, as a drop into another application does, pulses the
	/// dragged element; a move or a refused drop does not.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Drag_Pulses_The_Element_After_A_Copy(
		[Values(DragDropEffects.None, DragDropEffects.Copy, DragDropEffects.Move)] DragDropEffects effects)
	{
		// Arrange
		HighlightableBorder element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		Border target = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		DragDrop.SetAllowDrop(target, true);

		target.AddHandler(DragDrop.DropEvent, (_, e) => e.DragEffects = effects);

		StackPanel panel = new()
		{
			Children =
			{
				element,
				target
			},
			HorizontalAlignment = HorizontalAlignment.Left,
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Top
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(panel);

		// Act
		Drag(window, Start, OverTarget);

		// Assert
		element.Pulses
			.Should()
			.Be(effects == DragDropEffects.Copy ? 1 : 0);
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: the end of a drag is not announced while the button is still held.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Drag_Sends_No_Message_Before_The_Drop()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		window.MouseDown(Start, MouseButton.Left);

		// Act
		window.MouseMove(Start + Far, RawInputModifiers.LeftMouseButton);

		window.MouseMove(Start + Far + Far, RawInputModifiers.LeftMouseButton);

		Dispatcher.UIThread.RunJobs();

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: a move without the left button held starts no drag.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Move_Without_The_Left_Button_Starts_Nothing()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		window.MouseDown(Start, MouseButton.Left);

		// Act
		window.MouseMove(Start + Far);

		window.MouseUp(Start + Far, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: a press that an ancestor has already handled still starts the drag, as the
	/// header of a group handles the press it receives.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Press_Handled_By_An_Ancestor_Still_Drags()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		Panel ancestor = new()
		{
			Children =
			{
				element
			}
		};

		ancestor.AddHandler(
			InputElement.PointerPressedEvent,
			static (_, e) => e.Handled = true,
			RoutingStrategies.Tunnel);

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(ancestor);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		// Act
		Drag(window, Start, Start + Far);

		// Assert
		drags
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: a press in a popup of the element starts no drag, though its events pass
	/// through the element.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Press_In_A_Popup_Starts_Nothing()
	{
		// Arrange
		Border content = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		Popup popup = new()
		{
			Child = content
		};

		Panel element = new()
		{
			Background = Brushes.Transparent,
			Children =
			{
				popup
			},
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		popup.Open();

		Dispatcher.UIThread.RunJobs();

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		TopLevel popupRoot = TopLevel.GetTopLevel(content)!;

		Point press = content.TranslatePoint(Start, popupRoot)!.Value;

		// Act
		Drag(popupRoot, press, press + Far);

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: a release ends the gesture, so a later move with the button held starts
	/// no drag.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Release_Ends_The_Gesture()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		window.Click(Start);

		// Act
		window.MouseMove(Start + Far, RawInputModifiers.LeftMouseButton);

		window.MouseUp(Start + Far, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior" />: a press of the right button starts no drag.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Right_Press_Starts_Nothing()
	{
		// Arrange
		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = new ValueRecord()
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(element);

		List<DatasetRecordDragEndedMessage> drags = CaptureDrags();

		window.MouseDown(Start, MouseButton.Right);

		// Act
		window.MouseMove(Start + Far, RawInputModifiers.LeftMouseButton);

		window.MouseUp(Start + Far, MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		// Assert
		drags
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior.Record" />: a record with a blank text is dragged without a text.
	/// </summary>
	[AvaloniaTest]
	public void Record_Alone_Is_Dragged_Without_A_Text()
	{
		// Arrange
		ValueRecord record = new();

		DatasetRecordBase? droppedRecord = null;

		string? droppedText = null;

		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		Border target = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		DragDrop.SetAllowDrop(target, true);

		target.AddHandler(DragDrop.DropEvent, (_, e) =>
		{
			droppedRecord = e.DataTransfer.TryGetValue(DatasetRecordDragBehavior.RecordFormat);

			droppedText = e.DataTransfer.TryGetText();
		});

		StackPanel panel = new()
		{
			Children =
			{
				element,
				target
			},
			HorizontalAlignment = HorizontalAlignment.Left,
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Top
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = record,
			Text = " "
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(panel);

		// Act
		Drag(window, Start, OverTarget);

		// Assert
		droppedRecord
			.Should()
			.BeSameAs(record);

		droppedText
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DatasetRecordDragBehavior.Text" />: a text without a record is dragged alone.
	/// </summary>
	[AvaloniaTest]
	public void Text_Alone_Is_Dragged_Without_A_Record()
	{
		// Arrange
		string text = RandomString.Create(10);

		bool isDropped = false;

		DatasetRecordBase? droppedRecord = null;

		string? droppedText = null;

		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		Border target = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			Width = ElementWidth
		};

		DragDrop.SetAllowDrop(target, true);

		target.AddHandler(DragDrop.DropEvent, (_, e) =>
		{
			isDropped = true;

			droppedRecord = e.DataTransfer.TryGetValue(DatasetRecordDragBehavior.RecordFormat);

			droppedText = e.DataTransfer.TryGetText();
		});

		StackPanel panel = new()
		{
			Children =
			{
				element,
				target
			},
			HorizontalAlignment = HorizontalAlignment.Left,
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Top
		};

		DatasetRecordDragBehavior sut = new()
		{
			Record = null,
			Text = text
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Window window = Show(panel);

		// Act
		Drag(window, Start, OverTarget);

		// Assert
		isDropped
			.Should()
			.BeTrue();

		droppedRecord
			.Should()
			.BeNull();

		droppedText
			.Should()
			.Be(text);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Records every <see cref="DatasetRecordDragEndedMessage" />, one per drag that was started.
	/// </summary>
	private static List<DatasetRecordDragEndedMessage> CaptureDrags()
	{
		List<DatasetRecordDragEndedMessage> received = [];

		WeakReferenceMessenger
			.Default
			.Register<List<DatasetRecordDragEndedMessage>, DatasetRecordDragEndedMessage>(
				received,
				static (recipient, message) => recipient.Add(message));

		return received;
	}

	/// <summary>
	/// Presses the left button at <paramref name="from" />, moves to <paramref name="to" /> with the button held and
	/// releases it there.
	/// </summary>
	private static void Drag(TopLevel topLevel, Point from, Point to)
	{
		topLevel.MouseDown(from, MouseButton.Left);

		topLevel.MouseMove(to, RawInputModifiers.LeftMouseButton);

		// A drag that the first move has started follows the pointer from the next move on.
		topLevel.MouseMove(to, RawInputModifiers.LeftMouseButton);

		topLevel.MouseUp(to, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();
	}

	/// <summary>
	/// Shows an element in a window and lays it out.
	/// </summary>
	private static Window Show(Control element)
	{
		Window window = new()
		{
			Content = element,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Element that counts its highlight pulses.
	/// </summary>
	private sealed class HighlightableBorder : Border, IHighlightable
	{
		#region Properties
		/// <summary>
		/// Number of the pulses played.
		/// </summary>
		public int Pulses { get; private set; }
		#endregion

		#region Methods
		/// <inheritdoc />
		public void PulseHighlight() => Pulses++;
		#endregion
	}
	#endregion
}
