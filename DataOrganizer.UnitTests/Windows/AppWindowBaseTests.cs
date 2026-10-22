using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Platform;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.Windows;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(AppWindowBase)}"" type")]
internal class AppWindowBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppWindowBase.OnPropertyChanged" />: the size of a window not shown yet reaches its platform window.
	/// </summary>
	[AvaloniaTest]
	public void OnPropertyChanged_Hands_The_Size_Of_A_Window_Not_Shown_Yet_To_Its_Platform_Window()
	{
		// Act
		PlainWindow window = new()
		{
			Height = 480,
			Width = 640
		};

		// The headless platform resizes on the dispatcher.
		Dispatcher.UIThread.RunJobs();

		// Assert
		GetPlatformClientSize(window)
			.Should()
			.Be(new Size(640, 480));
	}

	/// <summary>
	/// <see cref="AppWindowBase.OnPropertyChanged" />: a window not shown yet that has a width but no height leaves its
	/// platform window as it is.
	/// </summary>
	[AvaloniaTest]
	public void OnPropertyChanged_Leaves_The_Platform_Window_Of_A_Window_Without_A_Height()
	{
		// Arrange
		PlainWindow window = new();

		Size initial = GetPlatformClientSize(window);

		// Act
		window.Width = 640;

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetPlatformClientSize(window)
			.Should()
			.Be(initial);
	}

	/// <summary>
	/// <see cref="AppWindowBase.OnPropertyChanged" />: a shown window leaves its new size to the layout.
	/// </summary>
	[AvaloniaTest]
	public void OnPropertyChanged_Leaves_The_Size_Of_A_Shown_Window_To_The_Layout()
	{
		// Arrange
		PlainWindow window = new()
		{
			Height = 480,
			Width = 640
		};

		window.Show();

		// Act
		window.Width = 700;

		// Assert (no layout pass has run yet)
		window.ClientSize
			.Should()
			.Be(new Size(640, 480));

		window.Close();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the client size the platform window of <paramref name="window" /> has, which Avalonia's reference assemblies
	/// hide.
	/// </summary>
	private static Size GetPlatformClientSize(Window window)
	{
		return (Size)typeof(ITopLevelImpl)
			.GetProperty("ClientSize")!
			.GetValue(window.PlatformImpl)!;
	}
	#endregion
}
