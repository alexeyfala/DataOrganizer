using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DataOrganizer.Behaviors.Input;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;
using System.Threading;
using System.Windows.Input;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(PointerHoverCommandBehavior)}"" type")]
internal class PointerHoverCommandBehaviorTests
{
	#region Data
	/// <summary>
	/// Hover delay of the behavior under test, in milliseconds.
	/// </summary>
	private const int Delay = 400;

	/// <summary>
	/// A point over the hover target.
	/// </summary>
	private static readonly Point Inside = new(25.0, 25.0);

	/// <summary>
	/// A point in the window away from the hover target.
	/// </summary>
	private static readonly Point Outside = new(150.0, 150.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="PointerHoverCommandBehavior.Command" />: a command that cannot execute is not run.
	/// </summary>
	[AvaloniaTest]
	public void Command_Is_Skipped_When_It_Cannot_Execute()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(false);

		(Window window, _, _) = CreateSetup(command, time);

		window.MouseMove(Inside);

		// Act
		time.Advance(TimeSpan.FromMilliseconds(Delay));

		// Assert
		command
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="PointerHoverCommandBehavior" />: a pointer that has left and come back runs the command after the delay.
	/// </summary>
	[AvaloniaTest]
	public void Command_Runs_After_The_Pointer_Comes_Back()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		(Window window, Border target, _) = CreateSetup(command, time);

		window.MouseMove(Inside);

		window.MouseMove(Outside);

		// Act
		window.MouseMove(Inside);

		time.Advance(TimeSpan.FromMilliseconds(Delay));

		// Assert
		command
			.Received(1)
			.Execute(target);
	}

	/// <summary>
	/// <see cref="PointerHoverCommandBehavior" />: the command runs once with the element when the pointer has rested for the delay.
	/// </summary>
	[AvaloniaTest]
	public void Command_Runs_With_The_Element_Once_The_Pointer_Has_Rested()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		(Window window, Border target, _) = CreateSetup(command, time);

		window.MouseMove(Inside);

		// Act
		time.Advance(TimeSpan.FromMilliseconds(Delay));

		// Assert
		command
			.Received(1)
			.Execute(target);
	}

	/// <summary>
	/// <see cref="PointerHoverCommandBehavior.CommandParameter" />: a set parameter replaces the element.
	/// </summary>
	[AvaloniaTest]
	public void Command_Runs_With_The_Parameter_When_One_Is_Set()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		object parameter = new();

		(Window window, _, _) = CreateSetup(command, time, parameter);

		window.MouseMove(Inside);

		// Act
		time.Advance(TimeSpan.FromMilliseconds(Delay));

		// Assert
		command
			.Received(1)
			.Execute(parameter);
	}

	/// <summary>
	/// <see cref="PointerHoverCommandBehavior" />: detaching drops the pending command.
	/// </summary>
	[AvaloniaTest]
	public void Detaching_Cancels_The_Pending_Command()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		(Window window, _, PointerHoverCommandBehavior behavior) = CreateSetup(command, time);

		window.MouseMove(Inside);

		// Act
		behavior.Detach();

		time.Advance(TimeSpan.FromMilliseconds(Delay));

		// Assert
		command
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="PointerHoverCommandBehavior.Delay" />: a pointer that comes back counts the delay anew, not what was left of it.
	/// </summary>
	[AvaloniaTest]
	public void Entering_Again_Counts_The_Delay_Anew()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		(Window window, _, _) = CreateSetup(command, time);

		window.MouseMove(Inside);

		time.Advance(TimeSpan.FromMilliseconds(Delay * 0.75));

		window.MouseMove(Outside);

		window.MouseMove(Inside);

		// Act
		time.Advance(TimeSpan.FromMilliseconds(Delay * 0.75));

		// Assert
		command
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="PointerHoverCommandBehavior" />: a pointer that leaves before the delay drops the command.
	/// </summary>
	[AvaloniaTest]
	public void Leaving_Cancels_The_Pending_Command()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		(Window window, _, _) = CreateSetup(command, time);

		window.MouseMove(Inside);

		// Act
		window.MouseMove(Outside);

		time.Advance(TimeSpan.FromMilliseconds(Delay));

		// Assert
		command
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="PointerHoverCommandBehavior" />: a press on the element drops the command.
	/// </summary>
	[AvaloniaTest]
	public void Pressing_Cancels_The_Pending_Command()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		(Window window, _, _) = CreateSetup(command, time);

		window.MouseMove(Inside);

		// Act
		window.MouseDown(Inside, MouseButton.Left);

		window.MouseUp(Inside, MouseButton.Left);

		time.Advance(TimeSpan.FromMilliseconds(Delay));

		// Assert
		command
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a hover target in a shown window and attaches the behavior to it, timed by <paramref name="time" />.
	/// </summary>
	private static (Window Window, Border Target, PointerHoverCommandBehavior Behavior) CreateSetup(
		ICommand command,
		FakeTimeProvider time,
		object? parameter = null)
	{
		Border target = new()
		{
			Background = Brushes.Transparent,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Window window = new()
		{
			Content = target,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		PointerHoverCommandBehavior behavior = new()
		{
			Command = command,
			CommandParameter = parameter,
			Delay = Delay,
			RunOnce = (action, delay) => time.CreateTimer(_ => action(), null, delay, Timeout.InfiniteTimeSpan)
		};

		behavior.Attach(target);

		return (window, target, behavior);
	}
	#endregion
}
