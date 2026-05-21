using Avalonia;
using Avalonia.Controls;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

internal sealed class ConsoleWindowHost : IConsoleWindowHost
{
	#region Properties
	/// <inheritdoc />
	public ConsoleViewModel ViewModel { get; }
	#endregion

	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public ConsoleWindowHost(
		Application app,
		IAppEnvironment appEnvironment,
		IFileSystem fileSystem,
		IJsonSerializerWrapper jsonSerializer,
		IViewFactory viewFactory)
	{
		_app = app;

		_appEnvironment = appEnvironment;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_viewFactory = viewFactory;

		ViewModel = viewFactory.CreateViewModel<ConsoleViewModel>();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task ConfigureAndShowAsync()
	{
		ConsoleWindow window = _viewFactory.CreateWindow<ConsoleWindow>(ViewModel);

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {Strings.Console} - {AppUtils.AppVersion}";

		string settingsFilePath = _appEnvironment.GetSettingsFilePath(nameof(ConsoleWindowSettings));

		if (_fileSystem.IsFileExists(settingsFilePath)
			&& _jsonSerializer.FromFile<ConsoleWindowSettings>(settingsFilePath) is { } settings
			&& settings.IsNotDefault())
		{
			ViewModel.FontSize = settings.FontSize;

			ViewModel.IsWordWrap = settings.IsWordWrap;

			window.Position = new(settings.X, settings.Y);

			window.Topmost = settings.IsTopmost;

			window.WindowState = settings.WindowState == WindowState.Minimized
				? WindowState.Normal
				: settings.WindowState;

			if (window.WindowState != WindowState.Maximized)
			{
				window.Width = settings.Size.Width;

				window.Height = settings.Size.Height;
			}
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
				if (ViewModel.IsSaved)
				{
					return;
				}

				ConsoleWindowSettings settings = new()
				{
					FontSize = ViewModel.FontSize,
					IsTopmost = window.Topmost,
					IsWordWrap = ViewModel.IsWordWrap,
					WindowState = window.WindowState,
					Size = new((int)window.Width, (int)window.Height),
					X = window.Position.X,
					Y = window.Position.Y
				};

				_fileSystem.SerializeToJsonFile(
					settings,
					settingsFilePath,
					false);

				ViewModel.IsSaved = true;

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
	#endregion
}
