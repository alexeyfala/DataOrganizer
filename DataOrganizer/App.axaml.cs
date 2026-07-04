using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Explorer;
using DataOrganizer.Services;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Encryption;
using DataOrganizer.Services.Execution;
using DataOrganizer.Services.Explorer;
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
using Serilog.Exceptions;
using Serilog.Sinks.FileEx;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
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
	#region Properties
	/// <summary>
	/// Application-wide service provider. Set in <see cref="OnFrameworkInitializationCompleted" />.
	/// Consumed by XAML markup extensions and other contexts that cannot use constructor injection.
	/// </summary>
	public static IServiceProvider? Services { get; private set; }

	/// <summary>
	/// The application lifetime timer.
	/// </summary>
	public Stopwatch AppLifetimeTimer { get; } = new();
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		base.OnFrameworkInitializationCompleted();

		if (!this.IsDesktop(out IClassicDesktopStyleApplicationLifetime? desktop))
		{
			return;
		}

		desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

		AppLifetimeTimer.Start();

		ServiceProvider? serviceProvider = null;

		try
		{
			serviceProvider = ConfigureServices(AddDebugCommandLineArgs(desktop
				.Args
				.AsNotNull()));
		}
		catch (Exception ex)
		{
			Debug.Fail(ex.Message);

			string filePath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				$"{AppUtils.AppName}_Critical_Errors{AppUtils.TxtExtension}");

			File.AppendAllText(
				filePath,
				$"[{DateTime.Now}] {ex.Message} → {SerilogExtensions.GetSourceInfo()}" + Environment.NewLine);

			Process.Start(new ProcessStartInfo
			{
				FileName = filePath,
				UseShellExecute = true
			});
		}

		if (serviceProvider is null)
		{
			return;
		}

		Services = serviceProvider;

		ClipboardSensitivityMarkerWriter.Configure(serviceProvider.GetRequiredService<IClipboardAutoClear>());

		DataTemplates.Add(serviceProvider.GetRequiredService<ViewLocator>());

		_ = serviceProvider
			.GetRequiredService<IAppController>()
			.LaunchAppAsync();
	}
	#endregion

	#region Helpers
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
				AppUtils.AppName + AppUtils.SQLiteExtension);

			SqliteConnectionStringBuilder connectionBuilder = new()
			{
				DataSource = dataSource,
				Mode = SqliteOpenMode.ReadWriteCreate,
				RecursiveTriggers = false,
				DefaultTimeout = 30,
				Pooling = true
			};

			builder.UseSqlite(
				connectionBuilder.ToString(),
				x => x.MigrationsAssembly(SqliteDbContext.MigrationsAssemblyName));

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
			.MinimumLevel.Is(options.MinimumLogEventLevel)
			.WriteTo.Async(configure =>
			{
				string path = Path.Combine(
					provider.GetRequiredService<IAppEnvironment>().AppDataDirectoryPath,
					"Logs",
					AppUtils.TxtExtension);

				configure.FileEx(
					path: path,
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
			ConsoleViewModel viewModel = provider
				.GetRequiredService<IConsoleWindowHost>()
				.ViewModel;

			LogCallbackSink sink = new()
			{
				LogCallback = viewModel.WriteCallback
			};

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
		services.AddTransient<IAppPickerService, WindowsAppPickerService>();
		services.AddTransient<IClipboardAccessor, ClipboardAccessor>();
		services.AddTransient<IDataExchangeService, DataExchangeService>();
		services.AddTransient<IDialogService, DialogService>();
		services.AddTransient<IDirectoryAccessor, DirectoryAccessor>();
		services.AddTransient<IDispatcherAccessor, DispatcherAccessor>();
		services.AddTransient<IEncryptionService, EncryptionService>();
		services.AddTransient<IEventSimulator, EventSimulator>();
		services.AddTransient<IFileAssociationService, FileAssociationService>();
		services.AddTransient<IFileChangeTracker, FileChangeTracker>();
		services.AddTransient<IFileSystem, FileSystem>();
		services.AddTransient<IFileSystemPicker, FileSystemPicker>();
		services.AddTransient<IGlobalHook>(_ => new SimpleGlobalHook(globalHookType: GlobalHookType.Keyboard));
		services.AddTransient<IJsonSerializerWrapper, JsonSerializerWrapper>();
		services.AddTransient<ILinuxExplorerManager, LinuxExplorerManager>();
		services.AddTransient<INotificationService, NotificationService>();
		services.AddTransient<IProcessUtils, ProcessUtils>();
		services.AddTransient<IStorageAccessor, StorageAccessor>();
		services.AddTransient<ITaskExceptionHandler, TaskExceptionHandler>();
		services.AddTransient<IViewFactory, ViewFactory>();
		services.AddTransient<IViewLauncher, ViewLauncher>();
		services.AddTransient<IWindowsExplorerManager, WindowsExplorerManager>();
		services.AddTransient<IXmlSerializerWrapper, XmlSerializerWrapper>();
		#endregion

		#region View locator
		services.AddSingleton<ViewLocator>();
		services.AddSingleton<IViewCache>(x => x.GetRequiredService<ViewLocator>());
		#endregion

		#region Singletons
		services.AddDbContext<SqliteDbContext>(ConfigureDbContext);
		services.AddLazySingleton<IConsoleWindowHost, ConsoleWindowHost>();
		services.AddLazySingleton<IKeyboardInputHook, KeyboardInputHook>();
		services.AddSingleton<Application>(this);
		services.AddSingleton<IAppController, AppController>();
		services.AddSingleton<IAppEnvironment, AppEnvironment>();
		services.AddSingleton<IAppSettingsManager, AppSettingsManager>();
		services.AddSingleton<IClipboardAutoClear, ClipboardAutoClear>();
		services.AddSingleton<IClipboardGate, ClipboardGate>();
		services.AddSingleton<IClipboardLogPersistenceCoordinator, ClipboardLogPersistenceCoordinator>();
		services.AddSingleton<IClipboardLogService, ClipboardLogService>();
		services.AddSingleton<IClipboardLogStore, ClipboardLogStore>();
		services.AddSingleton<ICommandLineOptions>(_ => new CommandLineOptions(args));
		services.AddSingleton<IDbAccess, DbAccess>();
		services.AddSingleton<IDbContextService, DbContextService>();
		services.AddSingleton<IDispatcher>(Dispatcher.UIThread);
		services.AddSingleton(TimeProvider.System);
		services.AddSingleton<IEntityEncryption, EntityEncryption>();
		services.AddSingleton<IEntityLoader, EntityLoader>();
		services.AddSingleton<IExecutionEngine, ExecutionEngine>();
		services.AddSingleton<IExplorerModelBaseRepository, ExplorerModelBaseRepository>();
		services.AddSingleton<IFileRepository, FileRepository>();
		services.AddSingleton<IFolderRepository, FolderRepository>();
		services.AddSingleton<IGlobalExceptionHandler, GlobalExceptionHandler>();
		services.AddSingleton<IHotkeysRepository, HotkeysRepository>();
		services.AddSingleton<ILogger>(ConfigureLogger);
		services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
		#endregion

		#endregion

		#region ViewModels
		services.AddTransient<AppPickerViewModel>();
		services.AddTransient<BooleanAsyncResultViewModel>();
		services.AddTransient<ClipboardLogViewModel>();
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
		services.AddTransient<PasswordBoxViewModel>();
		services.AddTransient<PropertiesViewModel>();
		services.AddTransient<SelectedFavoritesViewModel>();
		services.AddTransient<SettingsViewModel>();
		services.AddTransient<ToastViewModel>();
		services.AddTransient<YesNoCancelBoxViewModel>();
		#endregion

		#region Views
		services.AddTransient<AppPickerView>();
		services.AddTransient<ConsoleWindow>();
		services.AddTransient<ClipboardLogWindow>();
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

		return services.BuildServiceProvider();
	}
	#endregion
}
