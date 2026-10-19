using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Updates;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Runtime;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Runtime;

public sealed class AppController : IAppController, IRecipient<ExitAppMessage>, IRecipient<RestartAppMessage>
{
	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IAppShutdown" />
	private readonly IAppShutdown _appShutdown;

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

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IExecutingFiles" />
	private readonly IExecutingFiles _executingFiles;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IGlobalExceptionHandler" />
	private readonly IGlobalExceptionHandler _globalExceptionHandler;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMainWindowLauncher" />
	private readonly IMainWindowLauncher _mainWindowLauncher;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;

	/// <inheritdoc cref="IProcessManager" />
	private readonly IProcessManager _processManager;

	/// <inheritdoc cref="IExecutionSandbox" />
	private readonly IExecutionSandbox _sandbox;

	/// <inheritdoc cref="ISecondaryWindowCloser" />
	private readonly ISecondaryWindowCloser _secondaryWindowCloser;

	/// <inheritdoc cref="IServiceProvider" />
	private readonly IServiceProvider _serviceProvider;

	/// <inheritdoc cref="ISessionHierarchy" />
	private readonly ISessionHierarchy _sessionHierarchy;

	/// <inheritdoc cref="ISessionTreeState" />
	private readonly ISessionTreeState _sessionTreeState;

	/// <inheritdoc cref="IStartupDataLoader" />
	private readonly IStartupDataLoader _startupData;

	/// <inheritdoc cref="IStartupInfoLogger" />
	private readonly IStartupInfoLogger _startupInfo;

	/// <inheritdoc cref="IUpdateNotifier" />
	private readonly IUpdateNotifier _updateNotifier;

	/// <summary>
	/// <c>True</c> once a restart is asked for: the application starts again after it stops.
	/// </summary>
	private bool _isRestartAsked;
	#endregion

	#region Constructors
	public AppController(
		IAppEnvironment appEnvironment,
		IAppShutdown appShutdown,
		IAppUptime appUptime,
		IAvaloniaLogForwarder avaloniaLogForwarder,
		IBackgroundServices backgroundServices,
		ICommandLineOptions options,
		ICopyHistoryStore copyHistory,
		IExecutingFiles executingFiles,
		IExecutionSandbox sandbox,
		IFileSystem fileSystem,
		IGlobalExceptionHandler globalExceptionHandler,
		ILogger logger,
		IMainWindowLauncher mainWindowLauncher,
		IMessenger messenger,
		IProcessManager processManager,
		ISecondaryWindowCloser secondaryWindowCloser,
		IServiceProvider serviceProvider,
		ISessionHierarchy sessionHierarchy,
		ISessionTreeState sessionTreeState,
		IStartupDataLoader startupData,
		IStartupInfoLogger startupInfo,
		ITaskExceptionHandler exceptionHandler,
		IUpdateNotifier updateNotifier,
		Lazy<IConsoleWindowHost> consoleWindowHost)
	{
		_appEnvironment = appEnvironment;

		_appShutdown = appShutdown;

		_appUptime = appUptime;

		_avaloniaLogForwarder = avaloniaLogForwarder;

		_backgroundServices = backgroundServices;

		_consoleWindowHost = consoleWindowHost;

		_copyHistory = copyHistory;

		_executingFiles = executingFiles;

		_fileSystem = fileSystem;

		_globalExceptionHandler = globalExceptionHandler;

		_exceptionHandler = exceptionHandler;

		_logger = logger;

		_mainWindowLauncher = mainWindowLauncher;

		_options = options;

		_processManager = processManager;

		_sandbox = sandbox;

		_secondaryWindowCloser = secondaryWindowCloser;

		_serviceProvider = serviceProvider;

		_sessionHierarchy = sessionHierarchy;

		_sessionTreeState = sessionTreeState;

		_startupData = startupData;

		_startupInfo = startupInfo;

		_updateNotifier = updateNotifier;

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

			_startupInfo.Log();

			// TODO: Display a splash screen while connecting to database.

			if (await _startupData
				.LoadAsync(token)
				.ConfigureAwait(true) is not { } loaded)
			{
				// There is no window left to keep the application alive, and nothing to save.
				_appShutdown.Shutdown();

				return;
			}

			// TODO: Close splash screen here.

			_backgroundServices.Start(token);

			_sessionTreeState.Remember(loaded);

			_sessionHierarchy.Hierarchy.AddRange(loaded.Items);

			_copyHistory.Load(_sessionHierarchy.Hierarchy);

			_mainWindowLauncher.ShowMainWindow();

			_exceptionHandler.Watch(_updateNotifier.NotifyIfUpdateAvailableAsync(token));
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public void Receive(ExitAppMessage message) => _exceptionHandler.Watch(StopAppAsync());

	/// <inheritdoc />
	public void Receive(RestartAppMessage message) => _isRestartAsked = true;
	#endregion

	#region Helpers
	/// <summary>
	/// Closes the executed files, erases the sandbox and disposes the services.
	/// </summary>
	private async Task ReleaseSessionAsync()
	{
		await _executingFiles
			.CloseAllAsync()
			.ConfigureAwait(false);

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
	/// Stops the background services, closes the secondary windows, releases the session, starts the application again when
	/// asked and ends it.
	/// </summary>
	private async Task StopAppAsync()
	{
		await _backgroundServices
			.StopAsync()
			.ConfigureAwait(true);

		await _secondaryWindowCloser
			.CloseSecondaryWindowsAsync()
			.ConfigureAwait(true);

		// The application ends on the UI thread.
		await ReleaseSessionAsync().ConfigureAwait(true);

		// The new instance starts once this one has released its session.
		if (_isRestartAsked && Environment.ProcessPath is { } path)
		{
			_processManager.StartProcess(path);
		}

		_appShutdown.Shutdown();
	}
	#endregion
}
