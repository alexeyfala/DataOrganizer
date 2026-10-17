using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Text;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Updates;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Runtime;
using Repository.Enums;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OSVersion = OSVersionExtension.OSVersion;

namespace DataOrganizer.Services.Runtime;

public sealed class AppController : IAppController, IRecipient<ExitAppMessage>
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IAppUptime" />
	private readonly IAppUptime _appUptime;

	/// <inheritdoc cref="IAvaloniaLogForwarder" />
	private readonly IAvaloniaLogForwarder _avaloniaLogForwarder;

	/// <inheritdoc cref="IBackgroundServices" />
	private readonly IBackgroundServices _backgroundServices;

	/// <inheritdoc cref="IConsoleWindowHost" />
	private readonly Lazy<IConsoleWindowHost> _consoleWindowHost;

	/// <inheritdoc cref="ICopyHistoryStore" />
	private readonly ICopyHistoryStore _copyHistory;

	/// <inheritdoc cref="IDbConnector" />
	private readonly IDbConnector _dbConnector;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IExecutionEngine" />
	private readonly IExecutionEngine _executionEngine;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IGlobalExceptionHandler" />
	private readonly IGlobalExceptionHandler _globalExceptionHandler;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMainWindowLauncher" />
	private readonly IMainWindowLauncher _mainWindowLauncher;

	/// <inheritdoc cref="INoticeLauncher" />
	private readonly INoticeLauncher _noticeLauncher;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;

	/// <inheritdoc cref="IProcessTerminator" />
	private readonly IProcessTerminator _processTerminator;

	/// <inheritdoc cref="IExecutionSandbox" />
	private readonly IExecutionSandbox _sandbox;

	/// <inheritdoc cref="ISecondaryWindowLauncher" />
	private readonly ISecondaryWindowLauncher _secondaryWindowLauncher;

	/// <inheritdoc cref="IServiceProvider" />
	private readonly IServiceProvider _serviceProvider;

	/// <inheritdoc cref="ISessionHierarchy" />
	private readonly ISessionHierarchy _sessionHierarchy;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;

	/// <inheritdoc cref="IUpdateNotifier" />
	private readonly IUpdateNotifier _updateNotifier;

	/// <inheritdoc cref="IUpdatePrompt" />
	private readonly IUpdatePrompt _updatePrompt;
	#endregion

	#region Constructors
	public AppController(
		Application app,
		IAppEnvironment appEnvironment,
		IAppSettingsStore settingsStore,
		IAppUptime appUptime,
		IAvaloniaLogForwarder avaloniaLogForwarder,
		IBackgroundServices backgroundServices,
		ICommandLineOptions options,
		ICopyHistoryStore copyHistory,
		IDbConnector dbConnector,
		IEntityLoader entityLoader,
		IExecutionEngine executionEngine,
		IExecutionSandbox sandbox,
		IFileSystem fileSystem,
		IGlobalExceptionHandler globalExceptionHandler,
		ILogger logger,
		IMainWindowLauncher mainWindowLauncher,
		IMessenger messenger,
		INoticeLauncher noticeLauncher,
		INotificationService notification,
		IProcessTerminator processTerminator,
		ISecondaryWindowLauncher secondaryWindowLauncher,
		IServiceProvider serviceProvider,
		ISessionHierarchy sessionHierarchy,
		ITaskExceptionHandler exceptionHandler,
		IUpdateNotifier updateNotifier,
		IUpdatePrompt updatePrompt,
		Lazy<IConsoleWindowHost> consoleWindowHost)
	{
		_app = app;

		_appEnvironment = appEnvironment;

		_appUptime = appUptime;

		_avaloniaLogForwarder = avaloniaLogForwarder;

		_backgroundServices = backgroundServices;

		_consoleWindowHost = consoleWindowHost;

		_copyHistory = copyHistory;

		_dbConnector = dbConnector;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_globalExceptionHandler = globalExceptionHandler;

		_exceptionHandler = exceptionHandler;

		_executionEngine = executionEngine;

		_logger = logger;

		_mainWindowLauncher = mainWindowLauncher;

		_noticeLauncher = noticeLauncher;

		_notification = notification;

		_options = options;

		_processTerminator = processTerminator;

		_sandbox = sandbox;

		_secondaryWindowLauncher = secondaryWindowLauncher;

		_serviceProvider = serviceProvider;

		_sessionHierarchy = sessionHierarchy;

		_settingsStore = settingsStore;

		_updateNotifier = updateNotifier;

		_updatePrompt = updatePrompt;

		messenger.RegisterAll(this);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task LaunchAppAsync(CancellationToken token = default)
	{
		try
		{
			_globalExceptionHandler.StartMonitoring();

			_avaloniaLogForwarder.StartForwarding();

			_fileSystem.CreateDirectory(_appEnvironment.AppDataDirectoryPath);

			await _sandbox
				.EraseAsync(token)
				.ConfigureAwait(true);

			if (_options.ShowConsole)
			{
				await _consoleWindowHost
					.Value
					.ConfigureAndShowAsync()
					.ConfigureAwait(true);
			}

			InitialPrint();

			// TODO: Display a splash screen while connecting to database.

			DbConnectionStatus status = await _dbConnector
				.ConnectAsync(token)
				.ConfigureAwait(true);

			if (status is DbConnectionStatus.SchemaTooOld or DbConnectionStatus.SchemaTooNew)
			{
				// The data is intact, and an empty hierarchy would read as a loss worth undoing.
				_logger.LogError($"The schema of the database is {status}, the launch ends.", breakInDebugger: false);

				await _noticeLauncher
					.ShowStartupErrorAsync(_dbConnector.GetDbFilePath())
					.ConfigureAwait(true);

				// There is no window left to keep the application alive, and nothing to save.
				EndApplication();

				return;
			}

			bool isConnected = status is DbConnectionStatus.Connected;

			if (!isConnected)
			{
				_logger.LogError("The database is unavailable, the launch continues without it.", breakInDebugger: false);

				_notification.ShowToast(Strings.DatabaseIsUnavailable);
			}

			// Nothing is read from a database that is not there: the toast above has already said so.
			ExplorerItemDtoBase[]? hierarchy = isConnected
				? await _entityLoader
					.LoadHierarchyAsync(token)
					.ConfigureAwait(true)
				: [];

			if (hierarchy is null)
			{
				_logger.LogError(
					"The database could not be read, the launch continues with an empty hierarchy.",
					breakInDebugger: false);

				_notification.ShowToast(Strings.FailedToReadDatabase);
			}
			else
			{
				FileDto[] unreadable = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

				if (unreadable.IsNotEmpty())
				{
					unreadable.ForEach(x =>
					{
						_logger.LogError(
							$@"Hotkeys of file ""{x.Name}"" ({x.Id}) could not be read.",
							breakInDebugger: false);
					});

					_notification.ShowToast(
						unreadable.GetUnreadableHotkeysPresentation(Strings.FailedToReadHotkeys));
				}
			}

			// TODO: Close splash screen here.

			_backgroundServices.Start(token);

			_sessionHierarchy.Hierarchy.AddRange(hierarchy ?? []);

			_copyHistory.Load(_sessionHierarchy.Hierarchy);

			Window? mainWindow = _mainWindowLauncher.CreateMainWindow();

			if (mainWindow is null)
			{
				return;
			}

			mainWindow.Show();

			_exceptionHandler.Watch(_updateNotifier.NotifyIfUpdateAvailableAsync(_updatePrompt, token));
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public void Receive(ExitAppMessage message) => _exceptionHandler.Watch(StopAppAsync());
	#endregion

	#region Helpers
	/// <summary>
	/// Ends the desktop application, or the process where there is no desktop.
	/// </summary>
	private void EndApplication()
	{
		if (_app.IsDesktop(out IClassicDesktopStyleApplicationLifetime? desktop))
		{
			desktop.Shutdown();

			return;
		}

		_processTerminator.Terminate();
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
			$"{AppInfo.AppDisplayName} ({Assembly.GetEntryAssembly().GetVersionWithSuffix()})");

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

	/// <summary>
	/// Closes the executed files, erases the sandbox and disposes the services.
	/// </summary>
	private async Task ReleaseSessionAsync()
	{
		Guid[] executingFiles = [.. _sessionHierarchy
			.Hierarchy
			.GetFilesBy(x => x.IsExecuting)
			.Select(x => x.Id)];

		if (executingFiles.IsNotEmpty())
		{
			await executingFiles
				.ForEachAsync(x => _executionEngine.CloseAsync(x))
				.ConfigureAwait(false);
		}

		await _sandbox
			.EraseAsync()
			.ConfigureAwait(false);

		_logger.LogInformationWithTemplate(
			$@"App life time is: {_appUptime.Elapsed:hh\:mm\:ss\.fff}{Environment.NewLine}{Environment.NewLine}");

		if (_serviceProvider is IAsyncDisposable asyncDisposable)
		{
			await asyncDisposable
				.DisposeAsync()
				.ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Stops the background services, closes the secondary windows, releases the session and ends the application.
	/// </summary>
	private async Task StopAppAsync()
	{
		await _backgroundServices
			.StopAsync()
			.ConfigureAwait(true);

		await _secondaryWindowLauncher
			.CloseSecondaryWindowsAsync()
			.ConfigureAwait(true);

		// The application ends on the UI thread.
		await ReleaseSessionAsync().ConfigureAwait(true);

		EndApplication();
	}
	#endregion
}
