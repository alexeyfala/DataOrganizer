using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Mouse gestures of headless tests and the points they aim at.
/// </summary>
internal static class PointerExtensions
{
	#region Methods
	/// <summary>
	/// Returns the middle of a control in the coordinates of the root.
	/// </summary>
	public static Point CenterOf(this Visual root, Visual target)
	{
		return target.TranslatePoint(
			new(
				target.Bounds.Width / 2.0,
				target.Bounds.Height / 2.0),
			root) ?? default;
	}

	/// <summary>
	/// Presses and releases a button of the mouse at a point of a top level.
	/// </summary>
	public static void Click(this TopLevel topLevel, Point point, MouseButton button = MouseButton.Left)
	{
		topLevel.MouseDown(point, button);

		topLevel.MouseUp(point, button);
	}

	/// <summary>
	/// Presses and releases the left button of the mouse over the middle of a control.
	/// </summary>
	public static void Click(this TopLevel topLevel, Visual target)
	{
		topLevel.Click(topLevel.CenterOf(target));
	}

	/// <summary>
	/// Presses the left button of the mouse at one point of a top level, moves the mouse to another and releases it there.
	/// </summary>
	public static void Drag(this TopLevel topLevel, Point from, Point to)
	{
		topLevel.MouseDown(from, MouseButton.Left);

		topLevel.MouseMove(to, RawInputModifiers.LeftMouseButton);

		topLevel.MouseUp(to, MouseButton.Left);
	}
	#endregion
}
