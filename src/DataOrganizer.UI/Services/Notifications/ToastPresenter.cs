using Avalonia;
using Avalonia.Platform;
using DataOrganizer.Infrastructure.Notifications.Interfaces;
using DataOrganizer.Shared.Common;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.UI.Helpers.Views;
using DataOrganizer.UI.Interfaces.Views;
using DataOrganizer.UI.ViewModels.Windows;
using DataOrganizer.UI.Windows;
using Serilog;
using System;

namespace DataOrganizer.UI.Services.Notifications;

/// <inheritdoc cref="IToastPresenter" />
public sealed class ToastPresenter : IToastPresenter
{
	#region Properties
	/// <inheritdoc />
	/// <remarks>
	/// A toast has a window of its own, so there is nothing that could take the surface away.
	/// </remarks>
	public bool CanShow => true;

	/// <inheritdoc />
	public bool IsPointerOverMessage => _window?.IsPointerOver == true;
	#endregion

	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <inheritdoc cref="IWindowFinder" />
	private readonly IWindowFinder _windowFinder;

	/// <summary>
	/// The window the shown message occupies; <c>null</c> while none is shown.
	/// </summary>
	private ToastWindow? _window;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="ToastPresenter" /> class.
	/// </summary>
	public ToastPresenter(
		ILogger logger,
		IViewFactory viewFactory,
		IWindowFinder windowFinder)
	{
		_logger = logger;

		_viewFactory = viewFactory;

		_windowFinder = windowFinder;
	}
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

			viewModel.Title = AppInfo.AppDisplayName;

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
			(_window ?? _windowFinder.FindWindow<ToastWindow>())?.Close();

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
		window.Position = WindowPlacementHelper.GetLowerRightPosition(
			screen.WorkingArea,
			PixelSize.FromSize(window.ClientSize, screen.Scaling));
	}
	#endregion
}
