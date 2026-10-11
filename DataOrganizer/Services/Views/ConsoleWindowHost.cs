using Avalonia;
using Avalonia.Controls;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
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

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <inheritdoc cref="ConsoleViewModel" />
	private readonly ConsoleViewModel _viewModel;
	#endregion

	#region Constructors
	public ConsoleWindowHost(
		Application app,
		IAppEnvironment appEnvironment,
		IFileSystem fileSystem,
		IJsonSerializer jsonSerializer,
		IViewFactory viewFactory)
	{
		_app = app;

		_appEnvironment = appEnvironment;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_viewFactory = viewFactory;

		_viewModel = viewFactory.CreateViewModel<ConsoleViewModel>();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task ConfigureAndShowAsync()
	{
		ConsoleWindow window = _viewFactory.CreateWindow<ConsoleWindow>(_viewModel);

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {Strings.Console} - {AppInfo.AppVersion}";

		string settingsFilePath = _appEnvironment.GetSettingsFilePath(nameof(ConsoleWindowSettings));

		if (_fileSystem.FileExists(settingsFilePath)
			&& _jsonSerializer.DeserializeFromFile<ConsoleWindowSettings>(settingsFilePath) is { } settings
			&& settings.IsNotDefault())
		{
			_viewModel.FontSize = settings.FontSize;

			_viewModel.WordWrap = settings.WordWrap;

			if (settings.Size is { Width: > 0, Height: > 0 })
			{
				window.Width = settings.Size.Width;

				window.Height = settings.Size.Height;
			}
			else
			{
				IViewLauncher.SetDefaultSize(window);
			}

			PixelPoint savedPosition = new(settings.X, settings.Y);

			if (IViewLauncher.IsWindowPositionOnScreen(window, savedPosition))
			{
				window.Position = savedPosition;
			}
			else
			{
				IViewLauncher.SetDefaultLocation(window);
			}

			window.Topmost = settings.IsTopmost;

			window.WindowState = settings.WindowState == WindowState.Minimized
				? WindowState.Normal
				: settings.WindowState;
		}
		else
		{
			IViewLauncher.SetDefaultLocation(window);

			IViewLauncher.SetDefaultSize(window);
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
					WindowState = window.Placement.WindowState,
					Size = new((int)window.Placement.Size.Width, (int)window.Placement.Size.Height),
					X = window.Placement.Position.X,
					Y = window.Placement.Position.Y
				};

				_fileSystem.SerializeToJsonFile(
					settings,
					settingsFilePath,
					false);

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
