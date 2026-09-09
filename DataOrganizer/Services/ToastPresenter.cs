using Avalonia;
using Avalonia.Platform;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using System;

namespace DataOrganizer.Services;

public sealed class ToastPresenter : IToastPresenter
{
	#region Data
	/// <summary>
	/// Distance the toast keeps from the edges of the screen.
	/// </summary>
	private const int Margin = 10;

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <summary>
	/// The window the shown message occupies; <c>null</c> while none is shown.
	/// </summary>
	private ToastWindow? _window;
	#endregion

	#region Constructors
	public ToastPresenter(
		Application app,
		ILogger logger,
		IViewFactory viewFactory)
	{
		_app = app;

		_logger = logger;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	/// <remarks>
	/// A toast has a window of its own, so there is nothing that could take the surface away.
	/// </remarks>
	public bool CanShow => true;

	/// <inheritdoc />
	public bool IsPointerOverMessage => _window?.IsPointerOver == true;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Post(string content)
	{
		try
		{
			Remove();

			ToastViewModel viewModel = _viewFactory.CreateViewModel<ToastViewModel>();

			ToastWindow window = _viewFactory.CreateWindow<ToastWindow>(viewModel);

			viewModel.Title = AppInfo.AppNameParted;

			viewModel.Message = content;

			if (window
				.Screens
				.Primary is not { } screen)
			{
				return;
			}

			window.Show();

			Place(window, screen);

			_window = window;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public void Remove()
	{
		try
		{
			// A window left over from an earlier message counts as well.
			(_window ?? _app.FindWindow<ToastWindow>())?.Close();

			_window = null;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Puts the window in the lower corner of the working area.
	/// </summary>
	private static void Place(ToastWindow window, Screen screen)
	{
		PixelSize screenSize = screen
			.WorkingArea
			.Size;

		PixelSize windowSize = PixelSize.FromSize(window.ClientSize, screen.Scaling);

		window.Position = new PixelPoint(
			screenSize.Width - (windowSize.Width + Margin),
			screenSize.Height - (windowSize.Height + Margin));
	}
	#endregion
}
