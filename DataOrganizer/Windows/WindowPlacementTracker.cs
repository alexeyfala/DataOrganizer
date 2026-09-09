using Avalonia;
using Avalonia.Controls;
using DataOrganizer.Interfaces;

namespace DataOrganizer.Helpers;

/// <summary>
/// Keeps the position and the size a window had in the <see cref="WindowState.Normal" /> state,
/// so they stay valid while the window is minimized or maximized.
/// </summary>
internal sealed class WindowPlacementTracker
{
	#region Properties
	/// <summary>
	/// Position of the window in the <see cref="WindowState.Normal" /> state.
	/// </summary>
	public PixelPoint Position => _position ?? _window.Position;

	/// <summary>
	/// Size of the window in the <see cref="WindowState.Normal" /> state.
	/// </summary>
	public Size Size => _size ?? new(_window.Width, _window.Height);

	/// <summary>
	/// State of the window, where <see cref="WindowState.Minimized" /> is reported
	/// as <see cref="WindowState.Normal" />.
	/// </summary>
	public WindowState WindowState => _window.WindowState == WindowState.Minimized
		? WindowState.Normal
		: _window.WindowState;
	#endregion

	#region Data
	/// <inheritdoc cref="Window" />
	private readonly Window _window;

	/// <summary>
	/// Tracked position, <c>null</c> until the first one is tracked.
	/// </summary>
	private PixelPoint? _position;

	/// <summary>
	/// Tracked size, <c>null</c> until the first one is tracked.
	/// </summary>
	private Size? _size;
	#endregion

	#region Constructors
	private WindowPlacementTracker(Window window, bool isSizeTracked)
	{
		_window = window;

		window.PositionChanged += Window_PositionChanged;

		if (!isSizeTracked)
		{
			return;
		}

		window.Resized += Window_Resized;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Window.PositionChanged" /> event handler.
	/// </summary>
	private void Window_PositionChanged(object? sender, PixelPointEventArgs e)
	{
		if (_window.WindowState != WindowState.Normal || !IViewLauncher.IsWindowPositionOnScreen(_window, e.Point))
		{
			return;
		}

		_position = e.Point;
	}

	/// <summary>
	/// <see cref="TopLevel.Resized" /> event handler.
	/// </summary>
	private void Window_Resized(object? sender, WindowResizedEventArgs e)
	{
		if (_window.WindowState != WindowState.Normal
			|| e.ClientSize.Width <= default(double)
			|| e.ClientSize.Height <= default(double))
		{
			return;
		}

		_size = e.ClientSize;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Starts tracking the placement of the window.
	/// </summary>
	/// <param name="sizeTracked"><c>False</c> for a window of a fixed size.</param>
	public static WindowPlacementTracker Attach(
		Window window,
		bool sizeTracked = true) => new(window, sizeTracked);
	#endregion
}
