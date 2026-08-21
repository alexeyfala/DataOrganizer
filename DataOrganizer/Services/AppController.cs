using Avalonia.Controls;
using Cysharp.Text;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Updates;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OSVersion = OSVersionExtension.OSVersion;

namespace DataOrganizer.Services;

public sealed class AppController : IAppController
{
	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IClipboardLogService" />
	private readonly IClipboardLogService _clipboardLog;

	/// <inheritdoc cref="IClipboardLogPersistenceCoordinator" />
	private readonly IClipboardLogPersistenceCoordinator _clipboardLogPersistence;

	/// <inheritdoc cref="IConsoleWindowHost" />
	private readonly Lazy<IConsoleWindowHost> _consoleWindowHost;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notificationService;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;

	/// <inheritdoc cref="IExecutionSandbox" />
	private readonly IExecutionSandbox _sandbox;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;

	/// <inheritdoc cref="IUpdateNotifier" />
	private readonly IUpdateNotifier _updateNotifier;

	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;
	#endregion

	#region Constructors
	public AppController(
		IAppEnvironment appEnvironment,
		IAppSettingsStore settingsStore,
		IClipboardLogService clipboardLog,
		IClipboardLogPersistenceCoordinator clipboardLogPersistence,
		ICommandLineOptions options,
		IDbAccess dbAccess,
		IEntityLoader entityLoader,
		IExecutionSandbox sandbox,
		IFileSystem fileSystem,
		IGlobalExceptionHandler globalExceptionHandler,
		ILogger logger,
		INotificationService notificationService,
		ITaskExceptionHandler exceptionHandler,
		IUpdateNotifier updateNotifier,
		IViewLauncher viewLauncher,
		Lazy<IConsoleWindowHost> consoleWindowHost)
	{
		_appEnvironment = appEnvironment;

		_clipboardLog = clipboardLog;

		_clipboardLogPersistence = clipboardLogPersistence;

		_consoleWindowHost = consoleWindowHost;

		_dbAccess = dbAccess;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_exceptionHandler = exceptionHandler;

		_logger = logger;

		_notificationService = notificationService;

		_options = options;

		_sandbox = sandbox;

		_settingsStore = settingsStore;

		_updateNotifier = updateNotifier;

		_viewLauncher = viewLauncher;

		globalExceptionHandler.StartMonitoring();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task LaunchAppAsync(CancellationToken token = default)
	{
		try
		{
			_fileSystem.CreateDirectory(_appEnvironment.AppDataDirectoryPath);

			await _sandbox
				.EraseAsync(token)
				.ConfigureAwait(true);

			if (_options.IsConsoleNeeded)
			{
				await _consoleWindowHost
					.Value
					.ConfigureAndShowAsync()
					.ConfigureAwait(true);
			}

			InitialPrint();

			// TODO: Display a splash screen while connecting to database.

			if (!await _dbAccess
				.ConnectAsync(token)
				.ConfigureAwait(true))
			{
				// The launch goes on, but the state of the database is now known to the user:
				// nothing written from here on reaches it.
				_logger.LogError("The database is unavailable, the launch continues without it.", assertDebug: false);

				_notificationService.ShowToast(Strings.DatabaseIsUnavailable);
			}

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

			_clipboardLogPersistence.Start();

			if (_settingsStore
				.Settings
				.TrackClipboardHistory)
			{
				_exceptionHandler.Watch(_clipboardLog.StartAsync(token));
			}

			Window? mainWindow = _viewLauncher.ConfigureMainWindow(hierarchy);

			mainWindow?.Show();

			if (mainWindow?.DataContext is not IUpdatePrompt updatePrompt)
			{
				return;
			}

			_exceptionHandler.Watch(_updateNotifier.NotifyIfUpdateAvailableAsync(updatePrompt, token));
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion

	#region Helpers
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
			$"{AppUtils.AppNameParted} ({Assembly.GetEntryAssembly().GetVersionWithSuffix()})");

		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		builder.AppendLine("System specifications:");

		const string os = "OS";

		builder.AppendLine($"{os} platform - {Environment.OSVersion.Platform}");

		if (OperatingSystem.IsMacOS())
		{
			builder.AppendLine($"{os} type - macOS {Environment.OSVersion.Version}");
		}

		if (OperatingSystem.IsLinux())
		{
			builder.AppendLine($"{os} type - Linux {Environment.OSVersion.Version}");
		}

		if (OperatingSystem.IsWindows())
		{
			builder.AppendLine($"{os} type - {OSVersion.GetOperatingSystem()} {OSVersion.GetOSVersion().Version}");
		}

		builder.AppendLine($"{os} architecture - {RuntimeInformation.OSArchitecture}");

		builder.AppendLine($"Process architecture - {RuntimeInformation.ProcessArchitecture}");

		builder.AppendLine($"Runtime identifier - {RuntimeInformation.RuntimeIdentifier}");

		builder.Append($".NET version - {RuntimeInformation.FrameworkDescription}");

		_logger.LogInformationWithTemplate(builder.ToString());

		_logger.LogInformationWithTemplate($"Application settings:{_settingsStore
			.Settings
			.GetPropertyValues(true)}");
	}
	#endregion
}
