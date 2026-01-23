using Avalonia;
using Avalonia.Threading;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Windows;
using Shared.Common;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class NotificationService : INotificationService
{
	#region Data
	/// <inheritdoc cref="App" />
	private readonly App _app;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public NotificationService(
		App app,
		IDispatcher dispatcher,
		IViewFactory viewFactory)
	{
		_app = app;

		_dispatcher = dispatcher;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ShowToast(string message)
	{
		_dispatcher.Post(async () =>
		{
			_app
				.FindWindow<ToastWindow>()?
				.Close();

			ToastWindow window = _viewFactory.CreateWindow<ToastWindow>();

			window
				.ViewModel
				.Title = AppUtils.AppName;

			window
				.ViewModel
				.Message = message;

			if (window.Screens.Primary is not { } screen)
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
		});
	}
	#endregion
}
