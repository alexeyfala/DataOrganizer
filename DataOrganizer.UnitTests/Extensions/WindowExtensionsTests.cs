using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Extensions;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(WindowExtensions)}"" type")]
internal class WindowExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="WindowExtensions.RestoreAndActivate" />: the window comes on top of the others.
	/// </summary>
	[AvaloniaTest]
	public void RestoreAndActivate_Brings_The_Window_On_Top()
	{
		// Arrange
		Window window = new();

		Window other = new();

		window.Show();

		other.Show();

		// Act
		window.RestoreAndActivate();

		// Assert
		Window[] windows = [window, other];

		Window.SortWindowsByZOrder(windows);

		windows[^1]
			.Should()
			.BeSameAs(window);
	}

	/// <summary>
	/// <see cref="WindowExtensions.RestoreAndActivate" />: only a minimized window changes its state, to the normal one.
	/// </summary>
	[AvaloniaTest]
	[TestCase(WindowState.Minimized, WindowState.Normal)]
	[TestCase(WindowState.Maximized, WindowState.Maximized)]
	public void RestoreAndActivate_Restores_Only_A_Minimized_Window(WindowState state, WindowState expected)
	{
		// Arrange
		Window window = new()
		{
			WindowState = state
		};

		window.Show();

		// Act
		window.RestoreAndActivate();

		// Assert
		window.WindowState
			.Should()
			.Be(expected);
	}
	#endregion
}
