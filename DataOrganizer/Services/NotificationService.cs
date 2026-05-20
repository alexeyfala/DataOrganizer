using Avalonia;
using Avalonia.Threading;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class NotificationService : INotificationService
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public NotificationService(
		Application app,
		IDispatcher dispatcher,
		ILogger logger,
		IViewFactory viewFactory)
	{
		_app = app;

		_dispatcher = dispatcher;

		_logger = logger;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ShowToast(string message)
	{
		_dispatcher.Post(async () =>
		{
			try
			{
				_app
					.FindWindow<ToastWindow>()?
					.Close();

				ToastViewModel viewModel = _viewFactory.CreateViewModel<ToastViewModel>();

				ToastWindow window = _viewFactory.CreateWindow<ToastWindow>(viewModel);

				viewModel.Title = AppUtils.AppName;

				viewModel.Message = message;

				if (window
					.Screens
					.Primary is not { } screen)
				{
					return;
				}

				window.Show();

				PixelSize screenSize = screen
					.WorkingArea
					.Size;

				PixelSize windowSize = PixelSize.FromSize(window.ClientSize, screen.Scaling);

				const int margin = 10;

				window.Position = new PixelPoint(
					screenSize.Width - (windowSize.Width + margin),
					screenSize.Height - (windowSize.Height + margin));

				await Task
					.Delay(TimeSpan.FromSeconds(3))
					.ConfigureAwait(true);

				while (window.IsPointerOver)
				{
					await Task
						.Delay(TimeSpan.FromSeconds(1))
						.ConfigureAwait(true);
				}

				window.Close();
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);
			}
		});
	}
	#endregion
}
