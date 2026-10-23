using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Behaviors.Hierarchy;
using DataOrganizer.UnitTests.Factories;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DataOrganizer.UnitTests.Behaviors.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(ExplorerItemSignalsBehavior)}"" type")]
internal class ExplorerItemSignalsBehaviorTests
{
	#region Data
	/// <summary>
	/// Item of the list that the tests ask to show.
	/// </summary>
	private const string Item = "Second";

	/// <summary>
	/// Items of the list.
	/// </summary>
	private static readonly string[] Items = ["First", Item, "Third"];
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ExplorerItemSignalsBehavior.ShowSignal" />: an object asked for in a window that loads only after the
	/// limit of the wait is left unselected.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowSignal_Leaves_The_Object_When_Its_Window_Loads_Too_Late()
	{
		// Arrange
		FakeTimeProvider time = new();

		ICommand selectCommand = Substitute.For<ICommand>();

		Subject<IReadOnlyList<object>> showSignal = new();

		ListBox list = new()
		{
			ItemsSource = Items
		};

		ExplorerItemSignalsBehavior sut = new()
		{
			SelectCommand = selectCommand,
			ShowSignal = showSignal,
			TimeProvider = time
		};

		Interaction
			.GetBehaviors(list)
			.Add(sut);

		Window window = new()
		{
			Content = list,
			Height = 200.0,
			Width = 300.0
		};

		showSignal.OnNext([Item]);

		// Act
		time.Advance(TimeSpan.FromSeconds(2));

		window.Show();

		// A selection, were it made, would come within this wait.
		_ = await Dispatcher
			.UIThread
			.RunJobsUntilAsync(
				() => selectCommand.ReceivedCalls().Any(),
				maxRepeats: 30);

		window.Close();

		// Assert
		selectCommand
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}

	/// <summary>
	/// <see cref="ExplorerItemSignalsBehavior.ShowSignal" />: an object asked for before its window has loaded is selected
	/// as the window loads, with no pause after it.
	/// </summary>
	[AvaloniaTest]
	public void ShowSignal_Selects_The_Object_As_Its_Window_Loads()
	{
		// Arrange
		ICommand selectCommand = Substitute.For<ICommand>();

		Subject<IReadOnlyList<object>> showSignal = new();

		ListBox list = new()
		{
			ItemsSource = Items
		};

		ExplorerItemSignalsBehavior sut = new()
		{
			SelectCommand = selectCommand,
			ShowSignal = showSignal,
			TimeProvider = new FakeTimeProvider()
		};

		Interaction
			.GetBehaviors(list)
			.Add(sut);

		Window window = new()
		{
			Content = list,
			Height = 200.0,
			Width = 300.0
		};

		showSignal.OnNext([Item]);

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		window.Close();

		// Assert
		selectCommand
			.Received(1)
			.Execute(Item);
	}

	/// <summary>
	/// <see cref="ExplorerItemSignalsBehavior.ShowSignal" />: an object asked for in a loaded window is selected at once.
	/// </summary>
	[AvaloniaTest]
	public void ShowSignal_Selects_The_Object_At_Once_In_A_Loaded_Window()
	{
		// Arrange
		ICommand selectCommand = Substitute.For<ICommand>();

		Subject<IReadOnlyList<object>> showSignal = new();

		ListBox list = new()
		{
			ItemsSource = Items
		};

		ExplorerItemSignalsBehavior sut = new()
		{
			SelectCommand = selectCommand,
			ShowSignal = showSignal,
			TimeProvider = new FakeTimeProvider()
		};

		Interaction
			.GetBehaviors(list)
			.Add(sut);

		Window window = new()
		{
			Content = list,
			Height = 200.0,
			Width = 300.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		showSignal.OnNext([Item]);

		window.Close();

		// Assert
		selectCommand
			.Received(1)
			.Execute(Item);
	}
	#endregion
}
