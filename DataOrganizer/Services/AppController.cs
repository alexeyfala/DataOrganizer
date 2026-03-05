using Cysharp.Text;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Windows;
using MapsterMapper;
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
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;

	/// <inheritdoc cref="IProcessUtils" />
	private readonly IProcessUtils _processUtils;

	/// <inheritdoc cref="IAppSettingsManager" />
	private readonly IAppSettingsManager _settingsManager;

	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;
	#endregion

	#region Constructors
	public AppController(
		IAppSettingsManager settingsManager,
		ICommandLineOptions options,
		IDbAccess dbAccess,
		IExceptionHandler exceptionHandler,
		IFileSystem fileSystem,
		ILogger logger,
		IMapper mapper,
		IProcessUtils processUtils,
		IViewLauncher viewLauncher,
		IEntityLoader entityLoader)
	{
		_dbAccess = dbAccess;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_logger = logger;

		_options = options;

		_processUtils = processUtils;

		_settingsManager = settingsManager;

		_viewLauncher = viewLauncher;

		exceptionHandler.StartMonitoring();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task LaunchAppAsync(ConsoleWindow? console, CancellationToken token = default)
	{
		try
		{
			_fileSystem.CreateDirectory(AppUtils.AppDataDirectoryPath);

			if (console is not null)
			{
				await ShowConsoleWindowAsync(console).ConfigureAwait(true);
			}

			InitialPrint();

			// TODO: Display a splash screen while connecting to database.

			await _dbAccess
				.ConnectAsync(useMigrations: false, token)
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
				.ConfigureMainWindow(hierarchy)
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

		_logger.LogInformationWithTemplate($"Application settings:{_settingsManager.Settings.GetPropertyValues(true)}");
	}

	/// <summary>
	/// Prepares and shows <see cref="ConsoleWindow" />.
	/// </summary>
	private Task ShowConsoleWindowAsync(ConsoleWindow window)
	{
		window
			.ViewModel
			.InjectReference(_processUtils);

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
