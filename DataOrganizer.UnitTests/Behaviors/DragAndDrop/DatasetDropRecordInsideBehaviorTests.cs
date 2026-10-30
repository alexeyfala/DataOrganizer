using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.DragAndDrop;
using DataOrganizer.ViewModels;
using DatasetRecords.Dto;
using DatasetRecords.Models;
using FileContents.Interfaces;
using FileContents.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Behaviors.DragAndDrop;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetDropRecordInsideBehavior)}"" type")]
internal class DatasetDropRecordInsideBehaviorTests
{
	#region Data
	/// <summary>
	/// Height of the element under the pointer.
	/// </summary>
	private const double ElementHeight = 40.0;

	/// <summary>
	/// Width of the element under the pointer.
	/// </summary>
	private const double ElementWidth = 100.0;

	/// <summary>
	/// A point in the lower half of the element under the pointer.
	/// </summary>
	private static readonly Point LowerHalf = new(50.0, 30.0);

	/// <summary>
	/// A point in the upper half of the element under the pointer.
	/// </summary>
	private static readonly Point UpperHalf = new(50.0, 10.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: the element of the behavior takes drops.
	/// </summary>
	[AvaloniaTest]
	public void AllowDrop_Is_Turned_On_For_The_Element()
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

		DatasetDropRecordInsideBehavior sut = new();

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		// Act
		Show(element);

		// Assert
		DragDrop.GetAllowDrop(element)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a drag that leaves the element takes its line away.
	/// </summary>
	[AvaloniaTest]
	public void DragLeave_Removes_The_Line()
	{
		// Arrange
		ValueRecord dragged = new();

		ValueRecord target = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = target,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged, target]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		element.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None));

		// Act
		element.RaiseEvent(new DragEventArgs(DragDrop.DragLeaveEvent, data, element, UpperHalf, KeyModifiers.None));

		// Assert
		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a record dragged over the upper half of another record gets a line
	/// above it, where the record would land.
	/// </summary>
	[AvaloniaTest]
	public void DragOver_Draws_A_Line_Above_The_Upper_Half_Of_A_Record()
	{
		// Arrange
		ValueRecord dragged = new();

		ValueRecord target = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = target,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged, target]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		DragEventArgs args = new(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None);

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.Move);

		args.Handled
			.Should()
			.BeTrue();

		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.ContainSingle()
			.Which
			.BorderThickness
			.Should()
			.Be(new Thickness(0.0, 2.0, 0.0, 0.0));
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: over an element without a record the dragged record gets a line below,
	/// as it would go to the end of the records.
	/// </summary>
	[AvaloniaTest]
	public void DragOver_Draws_A_Line_Below_An_Element_Without_A_Record()
	{
		// Arrange
		ValueRecord dragged = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		DragEventArgs args = new(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None);

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.Move);

		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.ContainSingle()
			.Which
			.BorderThickness
			.Should()
			.Be(new Thickness(0.0, 0.0, 0.0, 2.0));
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a record dragged over the lower half of another record gets a line
	/// below it, where the record would land.
	/// </summary>
	[AvaloniaTest]
	public void DragOver_Draws_A_Line_Below_The_Lower_Half_Of_A_Record()
	{
		// Arrange
		ValueRecord dragged = new();

		ValueRecord target = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = target,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged, target]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		DragEventArgs args = new(DragDrop.DragOverEvent, data, element, LowerHalf, KeyModifiers.None);

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.Move);

		args.Handled
			.Should()
			.BeTrue();

		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.ContainSingle()
			.Which
			.BorderThickness
			.Should()
			.Be(new Thickness(0.0, 0.0, 0.0, 2.0));
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a record dragged over a group gets a rounded frame around it, as the
	/// record would go inside.
	/// </summary>
	[AvaloniaTest]
	public void DragOver_Frames_A_Group()
	{
		// Arrange
		ValueRecord dragged = new();

		RecordsGroup group = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = group,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged, group]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		DragEventArgs args = new(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None);

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.Move);

		Border frame = AdornerLayer
			.GetAdornerLayer(element)!
			.Children
			.OfType<Border>()
			.Single();

		frame.BorderThickness
			.Should()
			.Be(new Thickness(2.0));

		frame.CornerRadius
			.Should()
			.Be(new CornerRadius(4.0));
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a pointer that moves over the element moves the one line it has.
	/// </summary>
	[AvaloniaTest]
	public void DragOver_Keeps_One_Line_While_The_Pointer_Moves()
	{
		// Arrange
		ValueRecord dragged = new();

		ValueRecord target = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = target,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged, target]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		element.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None));

		// Act
		element.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, data, element, LowerHalf, KeyModifiers.None));

		// Assert
		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.ContainSingle()
			.Which
			.BorderThickness
			.Should()
			.Be(new Thickness(0.0, 0.0, 0.0, 2.0));
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a drag of something else than a record, such as a text, is left to
	/// the element.
	/// </summary>
	[AvaloniaTest]
	public void DragOver_Leaves_A_Foreign_Drag_Alone()
	{
		// Arrange
		ValueRecord target = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = target,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [target]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.CreateText(RandomString.Create(10)));

		DragEventArgs args = new(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None)
		{
			DragEffects = DragDropEffects.Copy
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.Copy);

		args.Handled
			.Should()
			.BeFalse();

		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a record dragged over itself is refused, with no line.
	/// </summary>
	[AvaloniaTest]
	public void DragOver_Refuses_A_Record_Onto_Itself()
	{
		// Arrange
		ValueRecord dragged = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = dragged,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		DragEventArgs args = new(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None)
		{
			DragEffects = DragDropEffects.Move
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.None);

		args.Handled
			.Should()
			.BeTrue();

		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a behavior turned off takes its line away and leaves the drag to the
	/// element.
	/// </summary>
	[AvaloniaTest]
	public void DragOver_Removes_The_Line_When_Disabled()
	{
		// Arrange
		ValueRecord dragged = new();

		ValueRecord target = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = target,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged, target]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		element.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None));

		sut.IsEnabled = false;

		DragEventArgs args = new(DragDrop.DragOverEvent, data, element, UpperHalf, KeyModifiers.None);

		// Act
		element.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior.MoveCommand" />: a record dropped onto the upper half of another record is
	/// asked to go before it.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Asks_To_Move_The_Record_Above_The_Upper_Half_Of_A_Record()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ValueRecord third = new();

		ObservableCollection<DatasetRecordBase> records = [first, second, third];

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = first,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = move,
			Records = records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, third));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, UpperHalf, KeyModifiers.None);

		DatasetRecordMove expected = new()
		{
			Index = 0,
			Record = third,
			Target = records
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		move
			.Received(1)
			.Execute(expected);
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior.MoveCommand" />: a record dropped onto the lower half of another record is
	/// asked to go after it, and the behavior itself moves nothing.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Asks_To_Move_The_Record_Below_The_Lower_Half_Of_A_Record()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ValueRecord third = new();

		ObservableCollection<DatasetRecordBase> records = [first, second, third];

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = third,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = move,
			Records = records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, first));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None);

		DatasetRecordMove expected = new()
		{
			Index = 3,
			Record = first,
			Target = records
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.Move);

		args.Handled
			.Should()
			.BeTrue();

		records
			.Should()
			.Equal(first, second, third);

		move
			.Received(1)
			.Execute(expected);
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior.MoveCommand" />: a record dropped onto a group is asked to go to the end
	/// of the group.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Asks_To_Move_The_Record_Into_A_Group()
	{
		// Arrange
		ValueRecord dragged = new();

		RecordsGroup group = new()
		{
			Children = [new ValueRecord()]
		};

		ObservableCollection<DatasetRecordBase> records = [dragged, group];

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = group,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = move,
			Records = records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, UpperHalf, KeyModifiers.None);

		DatasetRecordMove expected = new()
		{
			Index = 1,
			Record = dragged,
			Target = group.Children
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		move
			.Received(1)
			.Execute(expected);
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior.MoveCommand" />: a record of a group dropped onto a record outside the group
	/// is asked to go to that place.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Asks_To_Move_The_Record_Out_Of_A_Group()
	{
		// Arrange
		ValueRecord target = new();

		ValueRecord dragged = new();

		RecordsGroup group = new()
		{
			Children = [dragged]
		};

		ObservableCollection<DatasetRecordBase> records = [target, group];

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = target,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = move,
			Records = records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None);

		DatasetRecordMove expected = new()
		{
			Index = 1,
			Record = dragged,
			Target = records
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		move
			.Received(1)
			.Execute(expected);
	}

	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabled" />: a behavior turned off moves nothing and leaves the drop to the element.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Does_Nothing_When_Disabled()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ObservableCollection<DatasetRecordBase> records = [first, second];

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = second,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			IsEnabled = false,
			MoveCommand = move,
			Records = records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, first));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None);

		// Act
		element.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		records
			.Should()
			.Equal(first, second);

		move
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a drop of something else than a record, such as a text, is left to
	/// the element.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Leaves_A_Foreign_Drag_Alone()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ObservableCollection<DatasetRecordBase> records = [first, second];

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = second,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = move,
			Records = records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.CreateText(RandomString.Create(10)));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None);

		// Act
		element.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		records
			.Should()
			.Equal(first, second);

		move
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior.MoveCommand" />: bound to the command of the dataset, a drop moves the
	/// record and saves the records.
	/// </summary>
	[AvaloniaTest]
	public async Task Drop_Moves_The_Record_Through_The_Dataset()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ValueRecord third = new();

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

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel dataset = mock.Create<DatasetEditorViewModel>();

		await dataset.ContainerLoaded();

		dataset
			.Records
			.AddRange([first, second, third]);

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = third,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = dataset.MoveRecordCommand,
			Records = dataset.Records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, first));

		// Act
		element.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None));

		await dataset
			.MoveRecordCommand
			.ExecutionTask!;

		// Assert
		dataset.Records
			.Should()
			.Equal(second, third, first);

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior.Records" />: a record that is not among the records, as one dragged from
	/// another dataset, is refused.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Refuses_A_Record_Of_Another_Dataset()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ObservableCollection<DatasetRecordBase> records = [first, second];

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = second,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = move,
			Records = records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, new ValueRecord()));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None)
		{
			DragEffects = DragDropEffects.Move
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.None);

		args.Handled
			.Should()
			.BeTrue();

		records
			.Should()
			.Equal(first, second);

		move
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a record dropped onto itself is refused and stays where it is.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Refuses_A_Record_Onto_Itself()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ObservableCollection<DatasetRecordBase> records = [first, second];

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = first,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = move,
			Records = records
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, first));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None)
		{
			DragEffects = DragDropEffects.Move
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.None);

		args.Handled
			.Should()
			.BeTrue();

		records
			.Should()
			.Equal(first, second);

		move
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior.Records" />: without the records nothing can be placed, so the drop is
	/// refused.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Refuses_Without_Records()
	{
		// Arrange
		ValueRecord dragged = new();

		ICommand move = Substitute.For<ICommand>();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = new ValueRecord(),
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			MoveCommand = move,
			Records = null
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		DragEventArgs args = new(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None)
		{
			DragEffects = DragDropEffects.Move
		};

		// Act
		element.RaiseEvent(args);

		// Assert
		args.DragEffects
			.Should()
			.Be(DragDropEffects.None);

		args.Handled
			.Should()
			.BeTrue();

		move
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="DatasetDropRecordInsideBehavior" />: a drop takes the line away.
	/// </summary>
	[AvaloniaTest]
	public void Drop_Removes_The_Line()
	{
		// Arrange
		ValueRecord dragged = new();

		ValueRecord target = new();

		Border element = new()
		{
			Background = Brushes.Transparent,
			DataContext = target,
			Height = ElementHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = ElementWidth
		};

		DatasetDropRecordInsideBehavior sut = new()
		{
			Records = [dragged, target]
		};

		Interaction
			.GetBehaviors(element)
			.Add(sut);

		Show(element);

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(DatasetRecordDragBehavior.RecordFormat, dragged));

		element.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, data, element, LowerHalf, KeyModifiers.None));

		// Act
		element.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, data, element, LowerHalf, KeyModifiers.None));

		// Assert
		AdornerLayer.GetAdornerLayer(element)!.Children.OfType<Border>()
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows an element in a window and lays it out.
	/// </summary>
	private static void Show(Control element)
	{
		Window window = new()
		{
			Content = element,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();
	}
	#endregion
}
