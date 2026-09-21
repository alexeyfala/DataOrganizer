using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DataOrganizer.Interfaces.Views;

namespace DataOrganizer.Windows;

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
	/// <remarks>
	/// A window in the normal state carries the wanted size itself, the tracked one answers for the other states.
	/// </remarks>
	public Size Size => _window.WindowState == WindowState.Normal || _size is null
		? new(_window.Width, _window.Height)
		: _size.Value;

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
	/// <c>True</c> while a reported size waits for the state of the window.
	/// </summary>
	private bool _isSizePending;

	/// <summary>
	/// Tracked position, <c>null</c> until the first one is tracked.
	/// </summary>
	private PixelPoint? _position;

	/// <summary>
	/// Size reported by the last resize.
	/// </summary>
	private Size _resizedSize;

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
	/// <see cref="WindowBase.PositionChanged" /> event handler.
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
	/// <see cref="WindowBase.Resized" /> event handler.
	/// </summary>
	private void Window_Resized(object? sender, WindowResizedEventArgs e)
	{
		if (e.ClientSize.Width <= default(double) || e.ClientSize.Height <= default(double))
		{
			return;
		}

		_resizedSize = e.ClientSize;

		if (_isSizePending)
		{
			return;
		}

		_isSizePending = true;

		// A window reports its new size before its new state, so the state is read one dispatcher turn later.
		Dispatcher
			.UIThread
			.Post(TrackResizedSize, DispatcherPriority.Background);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Starts tracking the placement of the window.
	/// </summary>
	/// <param name="window">Window to track.</param>
	/// <param name="sizeTracked"><c>False</c> for a window of a fixed size.</param>
	public static WindowPlacementTracker Attach(
		Window window,
		bool sizeTracked = true) => new(window, sizeTracked);

	/// <summary>
	/// Tracks the size of the last resize once the state of the window has settled.
	/// </summary>
	private void TrackResizedSize()
	{
		_isSizePending = false;

		if (_window.WindowState != WindowState.Normal)
		{
			return;
		}

		_size = _resizedSize;
	}
	#endregion
}
