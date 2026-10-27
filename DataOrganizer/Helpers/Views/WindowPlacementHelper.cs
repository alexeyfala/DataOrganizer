using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform;
using DataOrganizer.Extensions.Views;
using Settings.Dto;
using Settings.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Views;

/// <summary>
/// Places windows on the working areas of screens.
/// </summary>
internal static class WindowPlacementHelper
{
	#region Data
	/// <summary>
	/// Default gap between a corner placed window and the edges of the working area, in pixels.
	/// </summary>
	public const int CornerMargin = 10;

	/// <summary>
	/// The default size of a window.
	/// </summary>
	public static readonly System.Drawing.Size DefaultWindowSize = new(900, 600);

	/// <summary>
	/// Size of the area at the top left of a window that has to stay on a screen, so that the window can be dragged back.
	/// </summary>
	private static readonly PixelSize TitleBarSize = new(100, 30);
	#endregion

	#region Methods
	/// <summary>
	/// Restores and activates <paramref name="target" />, then places it in the center of the screen
	/// of <paramref name="owner" />.
	/// </summary>
	public static void BringToScreenCenter(Window target, Window owner)
	{
		target.RestoreAndActivate();

		// A maximized window fills its own screen and stays there.
		if (target.WindowState == WindowState.Maximized)
		{
			return;
		}

		PositionAtScreenCenter(target, owner);
	}

	/// <summary>
	/// Returns the position of a window in the lower right corner of a working area, a margin away from its edges.
	/// </summary>
	public static PixelPoint GetLowerRightPosition(
		PixelRect workingArea,
		PixelSize windowSize,
		int margin = CornerMargin)
	{
		return new PixelPoint(
			workingArea.X + workingArea.Width - (windowSize.Width + margin),
			workingArea.Y + workingArea.Height - (windowSize.Height + margin));
	}

	/// <summary>
	/// <c>True</c> when the title bar of a window at <paramref name="position" /> lies on the working area of a connected
	/// screen.
	/// </summary>
	public static bool IsOnScreen(Window window, PixelPoint position)
	{
		IReadOnlyList<Screen>? screens = window
			.Screens?
			.All;

		if (screens is null || screens.Count == 0)
		{
			return true;
		}

		PixelRect titleBar = new(position, TitleBarSize);

		return screens.Any(x => x.WorkingArea.Intersects(titleBar));
	}

	/// <summary>
	/// Places <paramref name="target" /> at the bottom-right corner of the screen
	/// that <paramref name="owner" /> currently lives on.
	/// </summary>
	public static void PositionAtScreenBottomRight(Window target, Window owner)
	{
		if ((owner.Screens?.ScreenFromWindow(owner) ?? owner.Screens?.Primary) is not { } screen)
		{
			return;
		}

		// 16 device-independent pixels of padding from the screen edge.
		const int marginDip = 16;

		target.Position = WindowPlacementHelper.GetLowerRightPosition(
			screen.WorkingArea,
			new PixelSize((int)(target.Width * screen.Scaling), (int)(target.Height * screen.Scaling)),
			(int)(marginDip * screen.Scaling));
	}

	/// <summary>
	/// Places <paramref name="target" /> in the center of the working area of the screen
	/// that <paramref name="owner" /> currently lives on.
	/// </summary>
	public static void PositionAtScreenCenter(Window target, Window owner)
	{
		if ((owner.Screens?.ScreenFromWindow(owner) ?? owner.Screens?.Primary) is not { } screen)
		{
			return;
		}

		// A shown window knows its outer size, a new one only the size it asks for.
		PixelSize size = PixelSize.FromSize(
			target.FrameSize ?? new Avalonia.Size(target.Width, target.Height),
			screen.Scaling);

		target.Position = screen
			.WorkingArea
			.CenterRect(new PixelRect(size))
			.Position;
	}

	/// <summary>
	/// Gives the window the stored size, state and topmost flag where the settings keep them, and the stored position when
	/// it lies on a screen; a window without settings or with a position off every screen goes to
	/// <paramref name="placeByDefault" /> instead.
	/// </summary>
	public static void Restore(Window window, PositionSettings? settings, Action<Window> placeByDefault)
	{
		if (settings is null)
		{
			placeByDefault(window);

			return;
		}

		if (settings is PositionSizeSettings { Size: { Width: > 0, Height: > 0 } size })
		{
			window.Width = size.Width;

			window.Height = size.Height;
		}

		PixelPoint position = new(settings.X, settings.Y);

		if (IsOnScreen(window, position))
		{
			window.Position = position;
		}
		else
		{
			placeByDefault(window);
		}

		if (settings is not WindowPlacementSettings placement)
		{
			return;
		}

		window.Topmost = placement.IsTopmost;

		// A window closed minimized comes back in the normal state.
		window.WindowState = placement.WindowState == WindowShowState.Minimized
			? WindowState.Normal
			: (WindowState)placement.WindowState;
	}

	/// <summary>
	/// Sets the default <see cref="Window.WindowStartupLocation" /> to the window.
	/// </summary>
	public static void SetDefaultLocation(Window window)
	{
		window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
	}

	/// <summary>
	/// Sets the default <see cref="Layoutable.Width" /> and <see cref="Layoutable.Height" /> to the window.
	/// </summary>
	public static void SetDefaultSize(Window window)
	{
		window.Width = DefaultWindowSize.Width;

		window.Height = DefaultWindowSize.Height;
	}
	#endregion
}
