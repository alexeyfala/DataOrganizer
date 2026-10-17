using Avalonia;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using Shared.Common;
using Shared.Properties;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Views;

internal sealed class ConsoleWindowHost : IConsoleWindowHost
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <inheritdoc cref="ConsoleViewModel" />
	private readonly ConsoleViewModel _viewModel;

	/// <inheritdoc cref="IWindowSettingsStore" />
	private readonly IWindowSettingsStore _windowSettings;
	#endregion

	#region Constructors
	public ConsoleWindowHost(
		Application app,
		IAppEnvironment appEnvironment,
		IViewFactory viewFactory,
		IWindowSettingsStore windowSettings)
	{
		_app = app;

		_appEnvironment = appEnvironment;

		_viewFactory = viewFactory;

		_viewModel = viewFactory.CreateViewModel<ConsoleViewModel>();

		_windowSettings = windowSettings;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task ConfigureAndShowAsync()
	{
		ConsoleWindow window = _viewFactory.CreateWindow<ConsoleWindow>(_viewModel);

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {Strings.Console} - {AppInfo.AppVersion}";

		// The stored size, when there is one, replaces the default.
		WindowPlacementHelper.SetDefaultSize(window);

		if (_windowSettings.Read<ConsoleWindowSettings>() is { } settings)
		{
			_viewModel.FontSize = settings.FontSize;

			_viewModel.WordWrap = settings.WordWrap;

			WindowPlacementHelper.Restore(window, settings, WindowPlacementHelper.SetDefaultLocation);
		}
		else
		{
			WindowPlacementHelper.SetDefaultLocation(window);
		}

		window.Closing += delegate
		{
			try
			{
				if (_viewModel.IsSaved)
				{
					return;
				}

				ConsoleWindowSettings settings = new()
				{
					FontSize = _viewModel.FontSize,
					IsTopmost = window.Topmost,
					WordWrap = _viewModel.WordWrap,
					WindowState = window.Placement.ShowState,
					Size = window.Placement.Size,
					X = window.Placement.Position.X,
					Y = window.Placement.Position.Y
				};

				_windowSettings.Write(settings);

				_viewModel.IsSaved = true;

				_app.CloseAllWindows();
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.ToStringDemystified());
			}
		};

		TaskCompletionSource source = new();

		window.Loaded += delegate
		{
			source.SetResult();
		};

		window.Show();

		return source.Task;
	}

	/// <inheritdoc />
	public void Write(string text) => _viewModel.WriteCallback(text);
	#endregion
}
