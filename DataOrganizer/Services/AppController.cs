using Cysharp.Text;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using OSVersionExtension;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IAppController" />
public sealed class AppController : IAppController
{
	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IClipboardHistoryService" />
	private readonly IClipboardHistoryService _clipboardHistory;

	/// <inheritdoc cref="IConsoleWindowHost" />
	private readonly Lazy<IConsoleWindowHost> _consoleWindowHost;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _handler;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;

	/// <inheritdoc cref="IAppSettingsManager" />
	private readonly IAppSettingsManager _settingsManager;

	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;
	#endregion

	#region Constructors
	public AppController(
		IAppEnvironment appEnvironment,
		IAppSettingsManager settingsManager,
		IClipboardHistoryService clipboardHistory,
		ICommandLineOptions options,
		IDbAccess dbAccess,
		IEntityLoader entityLoader,
		IExceptionHandler exceptionHandler,
		IFileSystem fileSystem,
		ILogger logger,
		ITaskExceptionHandler handler,
		IViewLauncher viewLauncher,
		Lazy<IConsoleWindowHost> consoleWindowHost)
	{
		_appEnvironment = appEnvironment;

		_clipboardHistory = clipboardHistory;

		_consoleWindowHost = consoleWindowHost;

		_dbAccess = dbAccess;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_handler = handler;

		_logger = logger;

		_options = options;

		_settingsManager = settingsManager;

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
				await _consoleWindowHost
					.Value
					.ConfigureAndShowAsync()
					.ConfigureAwait(true);
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

			_handler.Watch(_clipboardHistory.StartAsync(token));
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
