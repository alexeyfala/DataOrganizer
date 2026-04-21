using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using Mapster;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.DbContexts;
using Repository.Interfaces;
using Repository.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.FileEx;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using Shared.Services;
using SharpHook;
using SharpHook.Data;
using System;
using System.Diagnostics;
using System.IO;

namespace DataOrganizer;

/// <inheritdoc />
public sealed class App : Application
{
	#region Data
	/// <inheritdoc cref="Stopwatch" />
	private readonly Stopwatch _timer = new();

	/// <summary>
	/// A window designed for displaying logs.
	/// </summary>
	private ConsoleWindow? _console;

	/// <inheritdoc cref="ServiceProvider" />
	private ServiceProvider? _serviceProvider;
	#endregion

	#region Event Handlers
	/// <inheritdoc cref="IControlledApplicationLifetime.Exit" />
	private void Desktop_Exit(
		object? sender,
		ControlledApplicationLifetimeExitEventArgs e)
	{
		_timer.Stop();

		_serviceProvider?
			.GetRequiredService<ILogger>()
			.LogInformationWithTemplate($"App life time: {_timer.GetElapsedTime()}, exit with code: {e.ApplicationExitCode}{Environment.NewLine}{Environment.NewLine}");

		_serviceProvider?.Dispose();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		base.OnFrameworkInitializationCompleted();

		if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
		{
			return;
		}

		desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

		desktop.Exit += Desktop_Exit;

		_timer.Start();

		try
		{
			_serviceProvider = ConfigureServices(AddDebugCommandLineArgs(desktop.Args.AsNotNull()));
		}
		catch (Exception ex)
		{
			Debug.Fail(ex.Message);

			string appDirectory = IAppEnvironment.GetAppDataDirectoryPath();

			Directory.CreateDirectory(appDirectory);

			string filePath = Path.Combine(
				appDirectory,
				$"{AppUtils.AppNameAsOneWord}_Critical_Errors{AppUtils.TxtExtension}");

			File.AppendAllText(
				filePath,
				$"[{DateTime.Now}] {ex.Message} → {SerilogExtensions.GetSourceInfo()}" + Environment.NewLine);

			Process.Start(new ProcessStartInfo
			{
				FileName = filePath,
				UseShellExecute = true
			});
		}

		if (_serviceProvider is null)
		{
			return;
		}

		_ = _serviceProvider
			.GetRequiredService<IAppController>()
			.LaunchAppAsync(_console);
	}
	#endregion

	#region Service
	/// <summary>
	/// Adds command line arguments for Debug mode.
	/// </summary>
	private static string[] AddDebugCommandLineArgs(string[] args)
	{
		if (AppUtils.IsDebug)
		{
			return args
				//.AddHelpArg()
				//.AddFillObjectsArg()
				.AddConsoleArg()
				.AddDebugArg();
		}

		return args;
	}

	/// <summary>
	/// Configures <see cref="ConsoleWindow" />.
	/// </summary>
	private static ConsoleWindow ConfigureConsoleWindow(
		Application app,
		IAppEnvironment appEnvironment,
		ICommandLineOptions options,
		IFileSystem fileSystem,
		IJsonSerializerWrapper serializer,
		IViewFactory viewFactory,
		out LogCallbackSink sink)
	{
		ConsoleWindow window = viewFactory.CreateWindow<ConsoleWindow>();

		sink = new LogCallbackSink()
		{
			IgnoreDebugLevel = options.MinimumLogEventLevel != LogEventLevel.Debug,
			LogCallback = window.ViewModel.WriteCallback
		};

		window.Title = $"{appEnvironment.GetAppInstanceName()} - {Strings.Console} - {AppUtils.AppVersion}";

		string settingsFilePath = appEnvironment.GetSettingsFilePath(nameof(ConsoleWindowSettings));

		if (fileSystem.IsFileExists(settingsFilePath)
			&& serializer.FromFile<ConsoleWindowSettings>(settingsFilePath) is { } settings
			&& settings.IsNotDefault())
		{
			window.ViewModel.FontSize = settings.FontSize;

			window.ViewModel.IsWordWrap = settings.IsWordWrap;

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
				if (window
					.ViewModel
					.IsSaved)
				{
					return;
				}

				ConsoleWindowSettings settings = new()
				{
					FontSize = window.ViewModel.FontSize,
					IsTopmost = window.Topmost,
					IsWordWrap = window.ViewModel.IsWordWrap,
					WindowState = window.WindowState,
					Size = new((int)window.Width, (int)window.Height),
					X = window.Position.X,
					Y = window.Position.Y
				};

				fileSystem.SerializeToJsonFile(
					settings,
					settingsFilePath,
					false);

				window
					.ViewModel
					.IsSaved = true;

				app.CloseAllWindows();
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.ToStringDemystified());
			}
		};

		return window;
	}

	/// <summary>
	/// Configures <see cref="SqliteDbContext" />.
	/// </summary>
	private static void ConfigureDbContext(
		IServiceProvider provider,
		DbContextOptionsBuilder builder)
	{
		try
		{
			string directoryPath = provider
				.GetRequiredService<IAppEnvironment>()
				.DatabaseDirectoryPath;

			Directory.CreateDirectory(directoryPath);

			string dataSource = Path.Combine(
				directoryPath,
				AppUtils.AppNameAsOneWord + AppUtils.SQLiteExtension);

			SqliteConnectionStringBuilder connectionBuilder = new()
			{
				DataSource = dataSource,
				Mode = SqliteOpenMode.ReadWriteCreate,
				RecursiveTriggers = false,
				DefaultTimeout = 30,
				Pooling = true
			};

			builder.UseSqlite(connectionBuilder.ToString());

			//ILogger logger = provider.GetRequiredService<ILogger>();

			//builder.LogTo(logger.LogDebugWithTemplate);
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex.ToStringDemystified());
		}
	}

	/// <summary>
	/// Configures <see cref="Logger" />.
	/// </summary>
	private Logger ConfigureLogger(IServiceProvider provider)
	{
		ICommandLineOptions options = provider.GetRequiredService<ICommandLineOptions>();

		LoggerConfiguration configuration = new LoggerConfiguration()
			.Enrich.WithExceptionDetails()
			.Enrich.WithDemystifiedStackTraces()
			.MinimumLevel.Debug()
			.WriteTo.Async(configure =>
			{
				string path = Path.Combine(
					provider.GetRequiredService<IAppEnvironment>().AppDataDirectoryPath,
					"Logs",
					AppUtils.TxtExtension);

				configure.FileEx(
					path: Path.Combine(provider.GetRequiredService<IAppEnvironment>().AppDataDirectoryPath, "Logs", AppUtils.TxtExtension),
					periodFormat: "dd.MM.yyyy",
					restrictedToMinimumLevel: options.MinimumLogEventLevel,
					outputTemplate: $"[{{Timestamp:{AppUtils.LogTimestampFormat}}}] [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}",
					rollingInterval: RollingInterval.Day,
					retainedFileCountLimit: 10,
					encoding: TextHelper.Utf8Encoding,
					preserveLogFileName: false,
					rollOnEachProcessRun: false);
			});

		if (options.IsConsoleNeeded)
		{
			_console = ConfigureConsoleWindow(
				this,
				provider.GetRequiredService<IAppEnvironment>(),
				options,
				provider.GetRequiredService<IFileSystem>(),
				provider.GetRequiredService<IJsonSerializerWrapper>(),
				provider.GetRequiredService<IViewFactory>(),
				out LogCallbackSink sink);

			configuration
				.WriteTo
				.Async(config => config.Sink(sink));
		}

		return configuration.CreateLogger();
	}

	/// <summary>
	/// Configures application services.
	/// </summary>
	private ServiceProvider ConfigureServices(string[] args)
	{
		ServiceCollection services = [];

		#region Services

		services.AddMapster();

		#region Transients		
		services.AddTransient<IClipboardService, ClipboardService>();
		services.AddTransient<IDataExchangeService, DataExchangeService>();
		services.AddTransient<IDialogService, DialogService>();
		services.AddTransient<IEncryptionService, EncryptionService>();
		services.AddTransient<IEventSimulator, EventSimulator>();
		services.AddTransient<IFileAssociationService, FileAssociationService>();
		services.AddTransient<IFileChangeTracker, FileChangeTracker>();
		services.AddTransient<IFileSystem, FileSystem>();
		services.AddTransient<IFileSystemPicker, FileSystemPicker>();
		services.AddTransient<IGlobalHook>(_ => new SimpleGlobalHook(globalHookType: GlobalHookType.Keyboard));
		services.AddTransient<IJsonSerializerWrapper, JsonSerializerWrapper>();
		services.AddTransient<INotificationService, NotificationService>();
		services.AddTransient<IProcessUtils, ProcessUtils>();
		services.AddTransient<IViewFactory, ViewFactory>();
		services.AddTransient<IViewLauncher, ViewLauncher>();
		services.AddTransient<IViewModelExecutionService, ViewModelExecutionService>();
		services.AddTransient<IXmlSerializerWrapper, XmlSerializerWrapper>();
		#endregion

		#region Singletons
		services.AddDbContext<SqliteDbContext>(ConfigureDbContext);
		services.AddSingleton<Application>(this);
		services.AddSingleton<IAppController, AppController>();
		services.AddSingleton<IAppEnvironment, AppEnvironment>();
		services.AddSingleton<IAppSettingsManager, AppSettingsManager>();
		services.AddSingleton<ICommandLineOptions>(_ => new CommandLineOptions(args));
		services.AddSingleton<IDbAccess, DbAccess>();
		services.AddSingleton<IDbContextService, DbContextService>();
		services.AddSingleton<IDispatcher>(Dispatcher.UIThread);
		services.AddSingleton<IEntityEcryption, EntityEcryption>();
		services.AddSingleton<IEntityLoader, EntityLoader>();
		services.AddSingleton<IExceptionHandler, ExceptionHandler>();
		services.AddSingleton<IExecutionEngine, ExecutionEngine>();
		services.AddSingleton<IExplorerModelBaseRepository, ExplorerModelBaseRepository>();
		services.AddSingleton<IFilesRepository, FilesRepository>();
		services.AddSingleton<IFoldersRepository, FoldersRepository>();
		services.AddSingleton<IHotkeysRepository, HotkeysRepository>();
		services.AddSingleton<IKeyboardInputHook, KeyboardInputHook>();
		services.AddSingleton<ILogger>(ConfigureLogger);
		#endregion

		#endregion

		#region ViewModels
		services.AddTransient<BooleanAsyncResultViewModel>();
		services.AddTransient<ConsoleViewModel>();
		services.AddTransient<CopyHistoryViewModel>();
		services.AddTransient<DatasetEditorViewModel>();
		services.AddTransient<EditingFilesViewModel>();
		services.AddTransient<EditorViewModel>();
		services.AddTransient<EmbeddedFileEditorViewModel>();
		services.AddTransient<EntityCreationViewModel>();
		services.AddTransient<FavoritesViewModel>();
		services.AddTransient<HotkeysEditorViewModel>();
		services.AddTransient<ImportListSelectorViewModel>();
		services.AddTransient<KeyValueInputViewModel>();
		services.AddTransient<MultilineTextEditViewModel>();
		services.AddTransient<PropertiesViewModel>();
		services.AddTransient<SelectedFavoritesViewModel>();
		services.AddTransient<SettingsViewModel>();
		services.AddTransient<ToastViewModel>();
		services.AddTransient<YesNoCancelBoxViewModel>();
		#endregion

		#region Views
		services.AddTransient<ConsoleWindow>();
		services.AddTransient<DatasetEditorView>();
		services.AddTransient<EditorWindow>();
		services.AddTransient<EmbeddedFileEditorView>();
		services.AddTransient<EntityCreationView>();
		services.AddTransient<FavoritesWindow>();
		services.AddTransient<HotkeysEditorView>();
		services.AddTransient<ImportListSelectorView>();
		services.AddTransient<KeyValueInputView>();
		services.AddTransient<MultilineTextEditView>();
		services.AddTransient<PasswordBox>();
		services.AddTransient<PropertiesView>();
		services.AddTransient<SettingsView>();
		services.AddTransient<ToastWindow>();
		services.AddTransient<YesNoCancelBox>();
		#endregion

		ServiceProvider provider = services.BuildServiceProvider();

		Ioc.Default.ConfigureServices(provider);

		return provider;
	}
	#endregion
}
