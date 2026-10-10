using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Views;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(TitleBarView)}"" type")]
internal class TitleBarViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="TitleBarView.ButtonContent" />: the button stands in the bar and keeps the data context of the window.
	/// </summary>
	[AvaloniaTest]
	public void ButtonContent_Keeps_The_Data_Context_Of_The_Window()
	{
		// Arrange
		object dataContext = new();

		Button button = new();

		TitleBarView sut = new()
		{
			ButtonContent = button
		};

		Window window = Show(sut);

		// Act
		window.DataContext = dataContext;

		// Assert
		button.GetVisualAncestors()
			.Should()
			.Contain(sut);

		button.DataContext
			.Should()
			.BeSameAs(dataContext);
	}

	/// <summary>
	/// <see cref="Window.Title" />: the title of the window shows in the bar.
	/// </summary>
	[AvaloniaTest]
	public void Title_Reaches_The_Bar()
	{
		// Arrange
		const string title = "Window title";

		TitleBarView sut = new();

		Window window = Show(sut);

		// Act
		window.Title = title;

		// Assert
		sut.WindowTitle.Text
			.Should()
			.Be(title);
	}

	/// <summary>
	/// <see cref="WindowBase.Topmost" />: turning the switch on keeps the window on top.
	/// </summary>
	[AvaloniaTest]
	public void Topmost_Follows_The_Switch()
	{
		// Arrange
		TitleBarView sut = new();

		Window window = Show(sut);

		// Act
		// A click sets the current value, which keeps the binding in place.
		sut.TopmostSwitch.SetCurrentValue(ToggleButton.IsCheckedProperty, true);

		// Assert
		window.Topmost
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="WindowBase.Topmost" />: a window kept on top shows the switch turned on.
	/// </summary>
	[AvaloniaTest]
	public void Topmost_Reaches_The_Switch()
	{
		// Arrange
		TitleBarView sut = new();

		Window window = Show(sut);

		// Act
		window.Topmost = true;

		// Assert
		sut.TopmostSwitch.IsChecked
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="Window.WindowState" />: checking the maximize button maximizes the window.
	/// </summary>
	[AvaloniaTest]
	public void WindowState_Follows_The_Maximize_Button()
	{
		// Arrange
		TitleBarView sut = new();

		Window window = Show(sut);

		// Act
		// A click sets the current value, which keeps the binding in place.
		sut.MaximizeButton.SetCurrentValue(ToggleButton.IsCheckedProperty, true);

		// Assert
		window.WindowState
			.Should()
			.Be(WindowState.Maximized);
	}

	/// <summary>
	/// <see cref="Window.WindowState" />: a maximized window shows the maximize button checked.
	/// </summary>
	[AvaloniaTest]
	public void WindowState_Reaches_The_Maximize_Button()
	{
		// Arrange
		TitleBarView sut = new();

		Window window = Show(sut);

		// Act
		window.WindowState = WindowState.Maximized;

		// Assert
		sut.MaximizeButton.IsChecked
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the bar in a window and lets the layout settle.
	/// </summary>
	private static Window Show(TitleBarView bar)
	{
		Window window = new()
		{
			Content = bar
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
