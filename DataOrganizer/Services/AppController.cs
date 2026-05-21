using Avalonia;
using Avalonia.Controls;
using Cysharp.Text;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using OSVersionExtension;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IAppController" />
public sealed class AppController : IAppController
{
	#region Data
	/// <inheritdoc cref="Avalonia.Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <summary>
	/// Lazy reference to the singleton <see cref="ConsoleViewModel" /> shared with the log sink.
	/// </summary>
	private readonly Lazy<ConsoleViewModel> _consoleViewModel;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;

	/// <inheritdoc cref="IAppSettingsManager" />
	private readonly IAppSettingsManager _settingsManager;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;
	#endregion

	#region Constructors
	public AppController(
		Application app,
		IAppEnvironment appEnvironment,
		IAppSettingsManager settingsManager,
		ICommandLineOptions options,
		IDbAccess dbAccess,
		IEntityLoader entityLoader,
		IExceptionHandler exceptionHandler,
		IFileSystem fileSystem,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		IViewFactory viewFactory,
		IViewLauncher viewLauncher,
		Lazy<ConsoleViewModel> consoleViewModel)
	{
		_app = app;

		_appEnvironment = appEnvironment;

		_consoleViewModel = consoleViewModel;

		_dbAccess = dbAccess;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_options = options;

		_settingsManager = settingsManager;

		_viewFactory = viewFactory;

		_viewLauncher = viewLauncher;

		exceptionHandler.StartMonitoring();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task LaunchAppAsync(CancellationToken token = default)
	{
		try
		{
			_fileSystem.CreateDirectory(_appEnvironment.AppDataDirectoryPath);

			if (_options.IsConsoleNeeded)
			{
				await ConfigureAndShowConsoleAsync().ConfigureAwait(true);
			}

			InitialPrint();

			// TODO: Display a splash screen while connecting to database.

			await _dbAccess
				.ConnectAsync(token)
				.ConfigureAwait(true);

			if (_options.FillObjects)
			{
				const int total = 3;

				await _dbAccess.AddRandomObjectsAsync(
					folders: total,
					files: total,
					datasets: total,
					levels: total).ConfigureAwait(true);
			}

			ExplorerModelBaseDto[] hierarchy = await _entityLoader
				.LoadFromEmbeddedDbAsync(token)
				.ConfigureAwait(true);

			// TODO: Close splash screen here.

			_viewLauncher
				.ConfigureMainWindow(hierarchy)?
				.Show();
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Creates, configures and shows <see cref="ConsoleWindow" />.
	/// </summary>
	private Task ConfigureAndShowConsoleAsync()
	{
		ConsoleViewModel viewModel = _consoleViewModel.Value;

		ConsoleWindow window = _viewFactory.CreateWindow<ConsoleWindow>(viewModel);

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {Strings.Console} - {AppUtils.AppVersion}";

		string settingsFilePath = _appEnvironment.GetSettingsFilePath(nameof(ConsoleWindowSettings));

		if (_fileSystem.IsFileExists(settingsFilePath)
			&& _jsonSerializer.FromFile<ConsoleWindowSettings>(settingsFilePath) is { } settings
			&& settings.IsNotDefault())
		{
			viewModel.FontSize = settings.FontSize;

			viewModel.IsWordWrap = settings.IsWordWrap;

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
				if (viewModel.IsSaved)
				{
					return;
				}

				ConsoleWindowSettings settings = new()
				{
					FontSize = viewModel.FontSize,
					IsTopmost = window.Topmost,
					IsWordWrap = viewModel.IsWordWrap,
					WindowState = window.WindowState,
					Size = new((int)window.Width, (int)window.Height),
					X = window.Position.X,
					Y = window.Position.Y
				};

				_fileSystem.SerializeToJsonFile(
					settings,
					settingsFilePath,
					false);

				viewModel.IsSaved = true;

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

	/// <summary>
	/// Writes initial data to log.
	/// </summary>
	private void InitialPrint()
	{
		if (_options.PrintHelp)
		{
			_logger.LogInformationWithTemplate(_options.GetHelp());
		}

		_logger.LogInformationWithTemplate(
			$"{AppUtils.AppName} ({Assembly.GetEntryAssembly().GetVersionWithSuffix()})");

		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		builder.AppendLine("System specifications:");

		const string os = "OS";

		builder.AppendLine($"{os} platform - {Environment.OSVersion.Platform}");

		if (AppUtils.IsMacOs)
		{
			builder.AppendLine($"{os} type - macOS {Environment.OSVersion.Version}");
		}

		if (AppUtils.IsLinux)
		{
			builder.AppendLine($"{os} type - Linux {Environment.OSVersion.Version}");
		}

		if (AppUtils.IsWindows)
		{
			builder.AppendLine($"{os} type - {OSVersion.GetOperatingSystem()} {OSVersion.GetOSVersion().Version}");
		}

		builder.AppendLine($"{os} architecture - {RuntimeInformation.OSArchitecture}");

		builder.AppendLine($"Process architecture - {RuntimeInformation.ProcessArchitecture}");

		builder.AppendLine($"Runtime identifier - {RuntimeInformation.RuntimeIdentifier}");

		builder.Append($".NET version - {RuntimeInformation.FrameworkDescription}");

		_logger.LogInformationWithTemplate(builder.ToString());

		_logger.LogInformationWithTemplate($"Application settings:{_settingsManager
			.Settings
			.GetPropertyValues(true)}");
	}
	#endregion
}
