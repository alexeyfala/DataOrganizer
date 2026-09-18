using Avalonia;
using AwesomeAssertions;
using DataOrganizer.Helpers;

namespace DataOrganizer.UnitTests.Helpers;

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
	#endregion
}
