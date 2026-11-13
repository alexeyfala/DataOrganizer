using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Hierarchy.Interfaces;
using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using DataOrganizer.Infrastructure.AppInstance.Messages;
using DataOrganizer.Infrastructure.AppUpdates.Interfaces;
using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Infrastructure.WindowContracts.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.SystemIntegration.Clipboard.Interfaces;
using DataOrganizer.SystemIntegration.FileExecution.Interfaces;
using DataOrganizer.Workflows.AppLifecycle.Interfaces;
using Serilog;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Workflows.AppLifecycle.Services;

/// <inheritdoc cref="IAppController" />
public sealed class AppController :
	IAppController,
	IRecipient<ExitAppMessage>,
	IRecipient<RestartAppMessage>
{
	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IAppProcess" />
	private readonly IAppProcess _appProcess;

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

	/// <inheritdoc cref="IPlatform" />
	private readonly IPlatform _platform;

	/// <inheritdoc cref="IProcessLauncher" />
	private readonly IProcessLauncher _processLauncher;

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
		IAppProcess appProcess,
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
		IPlatform platform,
		IProcessLauncher processLauncher,
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

		_appProcess = appProcess;

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

		_platform = platform;

		_processLauncher = processLauncher;

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

			_fileSystem
				.Directory
				.CreateDirectory(_appEnvironment.AppDataDirectoryPath);

			// On Windows the folder of a user is already closed to the other accounts by its access rights.
			if (!_platform.IsWindows)
			{
				RestrictToOwner(_appEnvironment.AppDataDirectoryPath);
			}

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
	/// Lets only the owner into <paramref name="directoryPath" />, so that the other accounts of the machine cannot read
	/// the data; a failure only goes to the log.
	/// </summary>
	[UnsupportedOSPlatform("windows")]
	private void RestrictToOwner(string directoryPath)
	{
		try
		{
			_fileSystem
				.File
				.SetUnixFileMode(
					directoryPath,
					UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

			_logger.LogInformation($@"Folder ""{directoryPath}"" is open to its owner only");
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
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
		if (_isRestartAsked && _appProcess.ProcessPath is { Length: > 0 } path)
		{
			// The shared dotnet host runs the application from its entry assembly, so it gets that assembly again.
			string[] arguments = _appProcess.IsRunByDotnetHost() && _appProcess.GetEntryAssemblyPath() is { } entryAssemblyPath
				? [entryAssemblyPath]
				: [];

			_processLauncher.StartProcess(path, arguments);
		}

		_appShutdown.Shutdown();
	}
	#endregion
}
