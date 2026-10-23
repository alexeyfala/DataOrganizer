using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Views;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Windows;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Helpers.Views;

[TestFixture(Description = $@"Tests of ""{nameof(WindowPlacementHelper)}"" type")]
internal class WindowPlacementHelperTests
{
	#region Methods
	/// <summary>
	/// <see cref="WindowPlacementHelper.GetLowerRightPosition" />: verifies the supplied margin replaces the default one.
	/// </summary>
	[Test]
	public void GetLowerRightPosition_Applies_Explicit_Margin()
	{
		// Act
		PixelPoint result = WindowPlacementHelper.GetLowerRightPosition(new PixelRect(10, 20, 1000, 800), new PixelSize(200, 100), 5);

		// Assert
		result
			.Should()
			.Be(new PixelPoint(805, 715));
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.GetLowerRightPosition" />: verifies the window is put into the corner of the working area.
	/// </summary>
	[Test]
	public void GetLowerRightPosition_Puts_Window_Into_Lower_Right_Corner()
	{
		// Act
		PixelPoint result = WindowPlacementHelper.GetLowerRightPosition(new PixelRect(0, 0, 1920, 1080), new PixelSize(300, 100));

		// Assert
		result
			.Should()
			.Be(new PixelPoint(1610, 970));
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.GetLowerRightPosition" />: verifies a window growing with its content stays in the corner.
	/// </summary>
	[Test]
	public void GetLowerRightPosition_Shifts_Position_When_Window_Grows()
	{
		// Arrange
		PixelRect workingArea = new(0, 0, 1920, 1080);

		// Act
		PixelPoint small = WindowPlacementHelper.GetLowerRightPosition(workingArea, new PixelSize(300, 100));

		PixelPoint grown = WindowPlacementHelper.GetLowerRightPosition(workingArea, new PixelSize(500, 160));

		// Assert
		grown
			.Should()
			.Be(new PixelPoint(small.X - 200, small.Y - 60));
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.IsOnScreen" />: a window counts as on a screen while its title bar lies on the
	/// working area, also past the left or top edge, and not when it is far away.
	/// </summary>
	[AvaloniaTest]
	[TestCase(0, 0, true)]
	[TestCase(-50, 100, true)]
	[TestCase(-20000, -20000, false)]
	public void IsOnScreen_Finds_The_Title_Bar_On_A_Screen(int offsetX, int offsetY, bool expected)
	{
		// Arrange
		Window window = new();

		PixelPoint position = window.Screens!.Primary!.WorkingArea.TopLeft + new PixelVector(offsetX, offsetY);

		// Act
		bool result = WindowPlacementHelper.IsOnScreen(window, position);

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.Restore" />: the stored size, position, state and topmost flag are given to the
	/// window.
	/// </summary>
	[AvaloniaTest]
	public void Restore_Applies_The_Stored_Placement()
	{
		// Arrange
		WindowPlacementSettings settings = new()
		{
			IsTopmost = true,
			Size = new(640, 480),
			WindowState = WindowShowState.Maximized,
			X = 120,
			Y = 90
		};

		List<Window> placedByDefault = [];

		Window window = new();

		// Act
		WindowPlacementHelper.Restore(window, settings, placedByDefault.Add);

		// Assert
		window.Width
			.Should()
			.Be(640);

		window.Height
			.Should()
			.Be(480);

		window.Position
			.Should()
			.Be(new PixelPoint(120, 90));

		window.Topmost
			.Should()
			.BeTrue();

		window.WindowState
			.Should()
			.Be(WindowState.Maximized);

		placedByDefault
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.Restore" />: the window takes the stored state.
	/// </summary>
	[AvaloniaTest]
	[TestCase(WindowShowState.Normal, WindowState.Normal)]
	[TestCase(WindowShowState.Maximized, WindowState.Maximized)]
	[TestCase(WindowShowState.FullScreen, WindowState.FullScreen)]
	public void Restore_Gives_The_Window_The_Stored_State(WindowShowState stored, WindowState expected)
	{
		// Arrange
		WindowPlacementSettings settings = new()
		{
			IsTopmost = false,
			Size = new(640, 480),
			WindowState = stored,
			X = 120,
			Y = 90
		};

		// No row expects the minimized state, so every row shows the window leave it.
		Window window = new()
		{
			WindowState = WindowState.Minimized
		};

		// Act
		WindowPlacementHelper.Restore(window, settings, _ => { });

		// Assert
		window.WindowState
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.Restore" />: a window stored without a size keeps the size it has.
	/// </summary>
	[AvaloniaTest]
	public void Restore_Keeps_The_Size_Of_A_Window_Stored_Without_One()
	{
		// Arrange
		WindowPlacementSettings settings = new()
		{
			IsTopmost = false,
			Size = new(0, 0),
			WindowState = WindowShowState.Normal,
			X = 120,
			Y = 90
		};

		Window window = new()
		{
			Height = 200.0,
			Width = 300.0
		};

		// Act
		WindowPlacementHelper.Restore(window, settings, _ => { });

		// Assert
		window.Width
			.Should()
			.Be(300.0);

		window.Height
			.Should()
			.Be(200.0);
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.Restore" />: settings that keep only a position leave the size, the state and the
	/// topmost flag of the window as they are.
	/// </summary>
	[AvaloniaTest]
	public void Restore_Leaves_All_But_The_Position_Of_A_Window_That_Keeps_Only_Its_Position()
	{
		// Arrange
		PositionSettings settings = new()
		{
			X = 120,
			Y = 90
		};

		Window window = new()
		{
			Height = 200.0,
			Width = 300.0
		};

		// Act
		WindowPlacementHelper.Restore(window, settings, _ => { });

		// Assert
		window.Position
			.Should()
			.Be(new PixelPoint(120, 90));

		window.Width
			.Should()
			.Be(300.0);

		window.Height
			.Should()
			.Be(200.0);

		window.WindowState
			.Should()
			.Be(WindowState.Normal);

		window.Topmost
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.Restore" />: a window closed minimized comes back in the normal state.
	/// </summary>
	[AvaloniaTest]
	public void Restore_Opens_A_Window_Saved_Minimized_In_The_Normal_State()
	{
		// Arrange
		WindowPlacementSettings settings = new()
		{
			IsTopmost = false,
			Size = new(640, 480),
			WindowState = WindowShowState.Minimized,
			X = 120,
			Y = 90
		};

		Window window = new();

		// Act
		WindowPlacementHelper.Restore(window, settings, _ => { });

		// Assert
		window.WindowState
			.Should()
			.Be(WindowState.Normal);
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.Restore" />: a window stored off every screen gets the default placement instead
	/// of the stored position.
	/// </summary>
	[AvaloniaTest]
	public void Restore_Places_A_Window_Saved_Off_The_Screens_By_Default()
	{
		// Arrange
		WindowPlacementSettings settings = new()
		{
			IsTopmost = false,
			Size = new(640, 480),
			WindowState = WindowShowState.Normal,
			X = -20000,
			Y = -20000
		};

		List<Window> placedByDefault = [];

		Window window = new();

		// Act
		WindowPlacementHelper.Restore(window, settings, placedByDefault.Add);

		// Assert
		placedByDefault
			.Should()
			.Equal(window);

		window.Position
			.Should()
			.NotBe(new PixelPoint(-20000, -20000));
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.Restore" />: a window without stored settings gets the default placement and keeps
	/// its own size.
	/// </summary>
	[AvaloniaTest]
	public void Restore_Places_A_Window_Without_Settings_By_Default()
	{
		// Arrange
		List<Window> placedByDefault = [];

		Window window = new()
		{
			Height = 300.0,
			Width = 400.0
		};

		// Act
		WindowPlacementHelper.Restore(window, null, placedByDefault.Add);

		// Assert
		placedByDefault
			.Should()
			.Equal(window);

		window.Width
			.Should()
			.Be(400.0);

		window.Height
			.Should()
			.Be(300.0);
	}

	/// <summary>
	/// <see cref="WindowPlacementHelper.Restore" />: a window of the application not shown yet hands the stored size to its
	/// platform window before it moves to the stored position.
	/// </summary>
	[AvaloniaTest]
	[TestCase(typeof(ClipboardLogWindow))]
	[TestCase(typeof(ConsoleWindow))]
	[TestCase(typeof(EditorWindow))]
	[TestCase(typeof(FavoritesWindow))]
	[TestCase(typeof(NotepadWindow))]
	public void Restore_Sizes_A_Window_Not_Shown_Yet_Before_It_Moves(Type windowType)
	{
		// Arrange
		WindowPlacementSettings settings = new()
		{
			IsTopmost = false,
			Size = new(640, 480),
			WindowState = WindowShowState.Normal,
			X = 120,
			Y = 90
		};

		List<Size> sizesAtMove = [];

		Window window = (Window)Activator.CreateInstance(windowType)!;

		window.PositionChanged += (_, _) => sizesAtMove.Add(window.ClientSize);

		// Act
		WindowPlacementHelper.Restore(window, settings, static _ => { });

		// Assert
		sizesAtMove
			.Should()
			.Equal(new Size(640, 480));
	}
	#endregion
}
