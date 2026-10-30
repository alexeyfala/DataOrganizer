using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Windows;
using Settings.Enums;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(WindowPlacementTracker)}"" type")]
internal class WindowPlacementTrackerTests
{
	#region Methods
	/// <summary>
	/// <see cref="WindowPlacementTracker.ShowState" />: the state of the window is reported for the settings, a minimized
	/// window as a normal one.
	/// </summary>
	[AvaloniaTest]
	[TestCase(WindowState.Normal, WindowShowState.Normal)]
	[TestCase(WindowState.Minimized, WindowShowState.Normal)]
	[TestCase(WindowState.Maximized, WindowShowState.Maximized)]
	[TestCase(WindowState.FullScreen, WindowShowState.FullScreen)]
	public void ShowState_Reports_The_State_To_Store(WindowState state, WindowShowState expected)
	{
		// Arrange
		Window window = new()
		{
			WindowState = state
		};

		WindowPlacementTracker sut = WindowPlacementTracker.Attach(window);

		// Act
		WindowShowState result = sut.ShowState;

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
