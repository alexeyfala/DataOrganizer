using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Infrastructure.Settings.Enums;
using DataOrganizer.UI.Windows;

namespace DataOrganizer.UI.UnitTests.Windows;

/// <summary>
/// Tests of <see cref="WindowPlacementTracker" /> type.
/// </summary>
[TestFixture]
internal sealed class WindowPlacementTrackerTests
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
