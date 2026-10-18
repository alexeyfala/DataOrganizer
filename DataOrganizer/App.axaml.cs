using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Diagnostics;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Exchange;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Explorer;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Seeding;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Storage;
using DataOrganizer.Interfaces.Text;
using DataOrganizer.Interfaces.Updates;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Diagnostics;
using DataOrganizer.Services.Dialogs;
using DataOrganizer.Services.Documents;
using DataOrganizer.Services.Encryption;
using DataOrganizer.Services.Exchange;
using DataOrganizer.Services.Execution;
using DataOrganizer.Services.Explorer;
using DataOrganizer.Services.Hierarchy;
using DataOrganizer.Services.Hotkeys;
using DataOrganizer.Services.Notepad;
using DataOrganizer.Services.Notes;
using DataOrganizer.Services.Notifications;
using DataOrganizer.Services.Runtime;
using DataOrganizer.Services.Seeding;
using DataOrganizer.Services.Settings;
using DataOrganizer.Services.Storage;
using DataOrganizer.Services.Updates;
using DataOrganizer.Services.Views;
using DataOrganizer.Templates;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Views;
using DataOrganizer.Views.Dialogs;
using DataOrganizer.Views.Settings;
using DataOrganizer.Windows;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Repository.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;
using Serilog.Sinks.FileEx;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Services;
using SharpHook;
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
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void Initialize()
	{
		Bootstrap();

		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		base.OnFrameworkInitializationCompleted();

		if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
			|| Services is not { } serviceProvider)
		{
			return;
		}

		desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

		// The theme reads Application.Styles, therefore it is applied only after the XAML tree is loaded.
		serviceProvider
			.GetRequiredService<IAppThemeService>()
			.ApplyFromSettings();

		DataTemplates.Add(serviceProvider.GetRequiredService<DocumentTabTemplate>());

		_ = serviceProvider
			.GetRequiredService<IAppController>()
			.LaunchAppAsync();
	}

	/// <summary>
	/// Fills <paramref name="services" /> with every service of the application.
	/// </summary>
	internal void RegisterServices(IServiceCollection services, string[] args)
	{
		#region Services

		services.AddMapster();

		services.AddSingleton(CreateMapperConfig());

		services.AddRepository(GetDatabaseFilePath);

		services.AddGitHubReleasesClient();

		RegisterPlatformServices(services);

		#region Transients
		services.AddTransient<IAppPickerPrompt, DialogService>();
		services.AddTransient<IAppShutdown, DesktopAccessor>();
		services.AddTransient<IAppThemeService, AppThemeService>();
		services.AddTransient<IAppVersionProvider, AppVersionProvider>();
		services.AddTransient<IAvaloniaClipboard, AvaloniaClipboard>();
		services.AddTransient<IBackgroundServiceControl, BackgroundServices>();
		services.AddTransient<IBackgroundServices, BackgroundServices>();
		services.AddTransient<IClipboardAccessor, ClipboardAccessor>();
		services.AddTransient<IClipboardHistoryUnlocker, ClipboardHistoryUnlocker>();
		services.AddTransient<IClipboardLogSeeder, ClipboardLogSeeder>();
		services.AddTransient<IClipboardTextWriter, ClipboardTextWriter>();
		services.AddTransient<IConfirmationPrompt, DialogService>();
		services.AddTransient<IContentCipher, ContentCipher>();
		services.AddTransient<IContentCopier, ContentCopier>();
		services.AddTransient<IContentKeys, ContentVisibility>();
		services.AddTransient<IContentVisibility, ContentVisibility>();
		services.AddTransient<IDataExchangeService, DataExchangeService>();
		services.AddTransient<IDekCipher, EncryptionService>();
		services.AddTransient<IDeletionScenario, DeletionScenario>();
		services.AddTransient<IDialogCloser, DialogCloser>();
		services.AddTransient<IDialogHostFinder, DesktopAccessor>();
		services.AddTransient<IDialogHostCloser, DialogHostCloser>();
		services.AddTransient<IDialogService, DialogService>();
		services.AddTransient<IDirectoryAccessor, DirectoryAccessor>();
		services.AddTransient<IDispatcherAccessor, DispatcherAccessor>();
		services.AddTransient<IDocumentEncodingSwitcher, DocumentEncodingSwitcher>();
		services.AddTransient<IEditorStateStore, EditorStateStore>();
		services.AddTransient<IEmbeddedEditorFactory, EmbeddedEditorFactory>();
		services.AddTransient<IEncryptedContentWriter, EncryptedContentWriter>();
		services.AddTransient<IEncryptionFailureReporter, EncryptionFailureReporter>();
		services.AddTransient<IEntityLoader, EntityLoader>();
		services.AddTransient<IEntityPropertyWriter, EntityPropertyWriter>();
		// The file picker lists the exchange formats in this order.
		services.AddTransient<IExchangeFormat, JsonExchangeFormat>();
		services.AddTransient<IExchangeFormat, XmlExchangeFormat>();
		services.AddTransient<IExchangeFormat, SqliteExchangeFormat>();
		services.AddTransient<IExecutionSandbox, ExecutionSandbox>();
		services.AddTransient<IFileChangeTracker, FileChangeTracker>();
		services.AddTransient<IFileContentsReader, FileContentsReader>();
		services.AddTransient<IFileContentsWriter, FileContentsWriter>();
		services.AddTransient<IFileExecutionScenario, FileExecutionScenario>();
		services.AddTransient<IFileHotkeyEditor, FileHotkeyEditor>();
		services.AddTransient<IFilePreviewReader, FilePreviewReader>();
		services.AddTransient<IFileSystem, FileSystem>();
		services.AddTransient<IFileSystemPicker, FileSystemPicker>();
		services.AddTransient<IFileTextCopier, FileTextCopier>();
		services.AddTransient<IFileTextReader, FileTextReader>();
		services.AddTransient<IFolderCipher, FolderCipher>();
		services.AddTransient<IFolderContentsConverter, FolderContentsConverter>();
		services.AddTransient<IFolderProtection, FolderProtection>();
		services.AddTransient<IHierarchyEditor, HierarchyEditor>();
		services.AddTransient<IHotkeyContentCopier, HotkeyContentCopier>();
		services.AddTransient<IHotkeysEditingScenario, HotkeysEditingScenario>();
		services.AddTransient<IHotkeysPrompt, DialogService>();
		services.AddTransient<IImportModePrompt, DialogService>();
		services.AddTransient<IImportScenario, ImportScenario>();
		services.AddTransient<IJsonSerializer, SystemTextJsonSerializer>();
		services.AddTransient<IKeeperDekWriter, KeeperDekWriter>();
		services.AddTransient<IKeeperUnlocker, KeeperUnlocker>();
		services.AddTransient<IMainWindowActivator, ViewLauncher>();
		services.AddTransient<IMainWindowLauncher, ViewLauncher>();
		services.AddTransient<INoteCipher, NoteCipher>();
		services.AddTransient<INoteEditor, NoteEditor>();
		services.AddTransient<INotepadStore, NotepadStore>();
		services.AddTransient<INotepadTexts, NotepadTexts>();
		services.AddTransient<INoteReader, NoteReader>();
		services.AddTransient<INoticeLauncher, ViewLauncher>();
		services.AddTransient<IOpenFilesCloser, OpenFilesCloser>();
		services.AddTransient<IPasswordDekWrapper, EncryptionService>();
		services.AddTransient<IPasswordPrompt, DialogService>();
		services.AddTransient<IPngPainter, PngPainter>();
		services.AddTransient<IProcessManager, ProcessManager>();
		services.AddTransient<IProcessTerminator, ProcessTerminator>();
		services.AddTransient<IProtectionScenarios, ProtectionScenarios>();
		services.AddTransient<ISampleSeeder, SampleSeeder>();
		services.AddTransient<ISecondaryWindowLauncher, ViewLauncher>();
		services.AddTransient<ISeedingScenarios, SeedingScenarios>();
		services.AddTransient<ISessionDekWrapper, EncryptionService>();
		services.AddTransient<IStartupDataLoader, StartupDataLoader>();
		services.AddTransient<IStartupInfoLogger, StartupInfoLogger>();
		services.AddTransient<IStorageAccessor, StorageAccessor>();
		services.AddTransient<ITaskExceptionHandler, TaskExceptionHandler>();
		services.AddTransient<ITextInputPrompt, DialogService>();
		services.AddTransient<ITypingPauseFactory, TypingPauseFactory>();
		services.AddTransient<IUiTimer, UiTimer>();
		services.AddTransient<IUnreadableHotkeysFinder, UnreadableHotkeysFinder>();
		services.AddTransient<IUpdateCheckService, UpdateCheckService>();
		services.AddTransient<IUpdateNotifier, UpdateNotifier>();
		services.AddTransient<IUpdatePrompt, UpdatePromptRelay>();
		services.AddTransient<IViewFactory, ViewFactory>();
		services.AddTransient<IViewModelFactory, ViewFactory>();
		services.AddTransient<IWindowCloser, DesktopAccessor>();
		services.AddTransient<IWindowFinder, DesktopAccessor>();
		services.AddTransient<IWindowSettingsStore, WindowSettingsStore>();
		services.AddTransient<IXmlSerializer, SystemXmlSerializer>();
		#endregion

		#region View Locator
		services.AddSingleton<DocumentTabTemplate>();
		#endregion

		#region Singletons
		services.AddKeyedSingleton<IMessagePresenter<SnackbarContent>>(
			SnackbarHostIdentifiers.Main,
			static (x, key) => x.GetRequiredKeyedService<ISnackbarPresenter>(key));
		services.AddKeyedSingleton<IMessagePresenter<SnackbarContent>>(
			SnackbarHostIdentifiers.Notepad,
			static (x, key) => x.GetRequiredKeyedService<ISnackbarPresenter>(key));
		services.AddKeyedSingleton<ISessionKeyStore, SessionKeyStore>(ClipboardLogStore.SessionKeyStoreKey);
		services.AddKeyedSingleton<ISnackbarPresenter, SnackbarPresenter>(SnackbarHostIdentifiers.Main);
		services.AddKeyedSingleton<ISnackbarPresenter, SnackbarPresenter>(SnackbarHostIdentifiers.Notepad);
		services.AddLazySingleton<IConsoleWindowHost, ConsoleWindowHost>();
		services.AddLazySingleton<IKeyboardInputHook, KeyboardInputHook>();
		services.AddSingleton(TimeProvider.System);
		services.AddSingleton<Application>(this);
		services.AddSingleton<IAppController, AppController>();
		services.AddSingleton<IAppEnvironment, AppEnvironment>();
		services.AddSingleton<IAppSettingsStore, AppSettingsStore>();
		services.AddSingleton<IAppUptime, AppUptime>();
		services.AddSingleton<IAutoLockService, AutoLockService>();
		services.AddSingleton<IAvaloniaLogForwarder, AvaloniaLogForwarder>();
		services.AddSingleton<IClipboardAutoClear, ClipboardAutoClear>();
		services.AddSingleton<IClipboardGate, ClipboardGate>();
		services.AddSingleton<IClipboardHistory, ClipboardHistory>();
		services.AddSingleton<IClipboardLogPersistenceCoordinator, ClipboardLogPersistenceCoordinator>();
		services.AddSingleton<IClipboardLogMerger>(static x => x.GetRequiredService<ClipboardLogService>());
		services.AddSingleton<IClipboardLogRunner>(static x => x.GetRequiredService<ClipboardLogService>());
		services.AddSingleton<IClipboardLogService>(static x => x.GetRequiredService<ClipboardLogService>());
		services.AddSingleton<IClipboardLogStore, ClipboardLogStore>();
		services.AddSingleton<ICommandLineOptions>(_ => new CommandLineOptions(args));
		services.AddSingleton<IConsoleLog, ConsoleLog>();
		services.AddSingleton<ICopyHistory>(static x => x.GetRequiredService<CopyHistory>());
		services.AddSingleton<ICopyHistoryRecorder>(static x => x.GetRequiredService<CopyHistory>());
		services.AddSingleton<ICopyHistoryStore>(static x => x.GetRequiredService<CopyHistory>());
		services.AddSingleton<IDbFailureReporter, DbFailureReporter>();
		services.AddSingleton<IDispatcher>(Dispatcher.UIThread);
		services.AddSingleton<IEditorSplitStore, EditorSplitStore>();
		services.AddSingleton<IExecutingFiles, ExecutingFiles>();
		services.AddSingleton<IExecutionEngine, ExecutionEngine>();
		services.AddSingleton<IGlobalExceptionHandler, GlobalExceptionHandler>();
		services.AddSingleton<IGlobalHook>(_ => new SimpleGlobalHook());
		services.AddSingleton<IGlobalHookRunner, GlobalHookRunner>();
		services.AddSingleton<IInstanceRegistry, InstanceRegistry>();
		services.AddSingleton<ILogger>(ConfigureLogger);
		services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
		services.AddSingleton<INotepadSessionState, NotepadSessionState>();
		services.AddSingleton<INotificationService, NotificationService>();
		services.AddSingleton<ISessionEditorTabs>(static x => x.GetRequiredService<WorkSession>());
		services.AddSingleton<ISessionHierarchy>(static x => x.GetRequiredService<WorkSession>());
		services.AddSingleton<ISessionKeyStore, SessionKeyStore>();
		services.AddSingleton<ISessionReadOnlyMode>(static x => x.GetRequiredService<WorkSession>());
		services.AddSingleton<ISettingsCloseGuard>(static x => x.GetRequiredService<SettingsSession>());
		services.AddSingleton<ISettingsCommit>(static x => x.GetRequiredService<SettingsSession>());
		services.AddSingleton<ISettingsDraft>(static x => x.GetRequiredService<SettingsSession>());
		services.AddSingleton<ISettingsSessionState, SettingsSessionState>();
		services.AddSingleton<ISyntaxRegistry>(static _ => SyntaxRegistry.Instance);
		services.AddSingleton<IToastPresenter, ToastPresenter>();
		services.AddSingleton<IUiCultureService, UiCultureService>();
		services.AddSingleton<ClipboardLogService>();
		services.AddSingleton<CopyHistory>();
		services.AddSingleton<SettingsSession>();
		services.AddSingleton<WorkSession>();
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
		services.AddTransient<NotepadViewModel>();
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
		services.AddTransient<NotepadTabView>();
		services.AddTransient<NotepadWindow>();
		services.AddTransient<PasswordBoxView>();
		services.AddTransient<PropertiesView>();
		services.AddTransient<SettingsView>();
		services.AddTransient<ToastWindow>();
		services.AddTransient<YesNoCancelBoxView>();
		#endregion
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds command line arguments for Debug mode.
	/// </summary>
	private static string[] AddDebugCommandLineArgs(string[] args)
	{
		if (AppInfo.IsDebug)
		{
			return args
				//.AddHelpArg()
				.AddConsoleArg()
				.AddDebugArg();
		}

		return args;
	}

	/// <summary>
	/// Applies the UI culture taken from the application settings.
	/// A failure leaves the culture of the operating system in place.
	/// </summary>
	private static void ApplyUiCulture(IServiceProvider provider)
	{
		try
		{
			provider
				.GetRequiredService<IUiCultureService>()
				.Apply(provider
					.GetRequiredService<IAppSettingsStore>()
					.Settings
					.Language);
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex.ToStringDemystified());
		}
	}

	/// <summary>
	/// Creates the mapping rules that every <see cref="MapsterMapper.IMapper" /> of the application uses.
	/// </summary>
	private static TypeAdapterConfig CreateMapperConfig()
	{
		TypeAdapterConfig config = new();

		config.Apply(new EntityMappingRegister());

		if (AppInfo.IsDebug)
		{
			try
			{
				config.Compile();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToStringDemystified());

				// A break under a test runner would stop a batch debug run.
				if (!AppDomain
					.CurrentDomain
					.IsRunningFromNUnit())
				{
					Debugger.Break();
				}

				// Temporarily add .IgnoreNonMapped(true) to the problematic mapping,
				// then remove one property at a time using .Map(dest => dest.PropertyName, src => src.PropertyName)
				// until you find the one that breaks the compilation.
			}
		}

		return config;
	}

	/// <summary>
	/// Returns the path of the database file and creates its directory.
	/// </summary>
	private static string GetDatabaseFilePath(IServiceProvider provider)
	{
		string directoryPath = provider
			.GetRequiredService<IAppEnvironment>()
			.DatabaseDirectoryPath;

		Directory.CreateDirectory(directoryPath);

		return Path.Combine(
			directoryPath,
			AppInfo.AppName + KnownFileExtensions.Sqlite);
	}

	/// <summary>
	/// Fills <paramref name="services" /> with the services whose implementation
	/// depends on the operating system in use.
	/// </summary>
	private static void RegisterPlatformServices(IServiceCollection services)
	{
		if (OperatingSystem.IsWindows())
		{
			services.AddTransient<IAppPickerService, WindowsAppPickerService>();

			services.AddTransient<IAppResolver, WindowsAppResolver>();

			services.AddTransient<IExplorerManager, WindowsExplorerManager>();

			services.AddTransient<IFileAssociationService, FileAssociationService>();

			return;
		}

		services.AddTransient<IAppPickerService, UnsupportedAppPickerService>();

		services.AddTransient<IAppResolver, SystemAppResolver>();

		if (OperatingSystem.IsLinux())
		{
			services.AddTransient<IExplorerManager, LinuxExplorerManager>();

			return;
		}

		services.AddTransient<IExplorerManager, MacOSExplorerManager>();
	}

	/// <summary>
	/// Writes the critical error in a file and opens it.
	/// </summary>
	private static void ReportCriticalError(Exception ex)
	{
		Debug.Fail(ex.Message);

		string filePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			$"{AppInfo.AppName}_Critical_Errors{KnownFileExtensions.Txt}");

		File.AppendAllText(
			filePath,
			$"[{DateTime.Now}] {ex.Message} → {SerilogExtensions.GetSourceInfo()}" + Environment.NewLine);

		Process.Start(new ProcessStartInfo
		{
			FileName = filePath,
			UseShellExecute = true
		});
	}

	/// <summary>
	/// Configures the application services and applies the UI culture.
	/// </summary>
	/// <remarks>
	/// Runs before the XAML tree is loaded, because application-level styles and themes resolve
	/// localized strings at parse time. Nothing resolved here may depend on <see cref="ILogger" />
	/// or on the visual tree: neither exists yet. Skipped without a desktop lifetime, which is the
	/// case for headless tests and for the XAML previewer.
	/// </remarks>
	private void Bootstrap()
	{
		if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
		{
			return;
		}

		ServiceProvider serviceProvider;

		try
		{
			serviceProvider = ConfigureServices(AddDebugCommandLineArgs(desktop
				.Args
				.AsNotNull()));
		}
		catch (Exception ex)
		{
			ReportCriticalError(ex);

			return;
		}

		// The uptime counts from here.
		_ = serviceProvider.GetRequiredService<IAppUptime>();

		Services = serviceProvider;

		ApplyUiCulture(serviceProvider);
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
					KnownFileExtensions.Txt);

				configure.FileEx(
					path: path,
					periodFormat: "dd.MM.yyyy",
					restrictedToMinimumLevel: options.MinimumLogEventLevel,
					outputTemplate: $"[{{Timestamp:{LogDefaults.TimestampFormat}}}] [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}",
					rollingInterval: RollingInterval.Day,
					retainedFileCountLimit: 10,
					encoding: TextDefaults.Encoding,
					preserveLogFileName: false,
					rollOnEachProcessRun: false);
			});

		if (options.ShowConsole)
		{
			LogCallbackSink sink = new()
			{
				LogCallback = provider
					.GetRequiredService<IConsoleLog>()
					.Write
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

		RegisterServices(services, args);

		return services.BuildServiceProvider();
	}
	#endregion
}
