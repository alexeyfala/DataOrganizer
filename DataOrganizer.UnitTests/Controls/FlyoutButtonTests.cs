using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Controls;
using Material.Icons;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(FlyoutButton)}"" type")]
internal class FlyoutButtonTests
{
	#region Methods
	/// <summary>
	/// <see cref="FlyoutButton.Gesture" />: a button without a header shows the keys in its tip, after the tip text.
	/// </summary>
	[AvaloniaTest]
	public void Gesture_Joins_The_Tip_Of_A_Button_Without_A_Header()
	{
		// Arrange
		FlyoutButton sut = new()
		{
			Content = "X",
			Gesture = "Ctrl+X"
		};

		ToolTip.SetTip(sut, "Cut");

		// Act
		Show(sut);

		// Assert
		ToolTip.GetTip(sut)
			.Should()
			.Be("Cut (Ctrl+X)");
	}

	/// <summary>
	/// <see cref="FlyoutButton.Gesture" />: the keys of the rows of a menu stand in one column at its right edge,
	/// whatever the length of the headers.
	/// </summary>
	[AvaloniaTest]
	public void Gesture_Lines_Up_At_The_Right_Edge()
	{
		// Arrange
		FlyoutButton shortRow = new()
		{
			Gesture = "F2",
			Header = "Next",
			Icon = MaterialIconKind.ArrowDown
		};

		FlyoutButton longRow = new()
		{
			Gesture = "Ctrl+Shift+U",
			Header = "A much longer header",
			Icon = MaterialIconKind.Magnify
		};

		// Act
		Show(new StackPanel
		{
			Orientation = Orientation.Vertical,
			Children =
			{
				shortRow,
				longRow
			}
		});

		// Assert
		GetKeysRight(shortRow)
			.Should()
			.BeApproximately(GetKeysRight(longRow), 1.0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the right edge of the keys of a button in its own coordinates.
	/// </summary>
	private static double GetKeysRight(FlyoutButton button)
	{
		TextBlock keys = button
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Single(x => x.Text == button.Gesture);

		return keys.TranslatePoint(new(keys.Bounds.Width, 0.0), button)?.X ?? 0.0;
	}

	/// <summary>
	/// Shows the content in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static void Show(Control content)
	{
		Window window = new()
		{
			Content = content,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();
	}
	#endregion
}
