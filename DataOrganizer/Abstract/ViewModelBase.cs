using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using DataOrganizer.ViewModels;
using Material.Styles.Controls;
using Material.Styles.Models;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using SharpHook;
using SharpHook.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

/// <summary>
/// Base view model.
/// </summary>
public abstract partial class ViewModelBase :
	CopyContentViewModelBase,
	IRecipient<ShowSnackbarMessage>,
	IRecipient<CloseExecutingFileMessage>
{
	#region Properties
	/// <inheritdoc cref="CopyHistoryViewSettings" />
	public CopyHistoryViewSettings CopyHistorySettings { get; } = new();

	/// <summary>
	/// Executed in operating system files.
	/// </summary>
	public ObservableCollection<FileModelDto> ExecutingFiles { get; } = [];

	/// <summary>
	/// Hierarchical sequence of objects.
	/// </summary>
	public ObservableCollection<ExplorerModelBaseDto> Hierarchy { get; } = [];

	/// <summary>
	/// <c>True</c> when initialization has completed.
	/// </summary>
	public bool IsInitialized { get; protected set; }

	/// <summary>
	/// <c>True</c> when shutdown is requested.
	/// </summary>
	public bool IsShutdown { get; protected set; } = true;

	/// <summary>
	/// Opened in editor files.
	/// </summary>
	public List<FileModelDto> OpenedInEditorFiles { get; } = [];

	/// <summary>
	/// Snackbar's text color.
	/// </summary>
	[ObservableProperty]
	public partial IBrush? SnackbarForeground { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes a file executing in the operating system.
	/// </summary>
	[RelayCommand]
	internal void CloseExecutingFile(FileModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		_logger.LogInformation($"Closing an executed file in the operating system:{dto.GetPropertyValues(
			true,
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name),
			nameof(FileModelDto.EntityType))}");

		_dispatcher.Post(() =>
		{
			ExecutingFiles.Remove(dto);

			dto.IsExecuting = false;
		});

		_handler.Watch(_executionEngine.CloseAsync(dto.Id));
	}

	/// <summary>
	/// Handles the display of copy history.
	/// </summary>
	[RelayCommand]
	private void CopyHistoryDisplayed(CopyHistoryViewModel? viewModel)
	{
		viewModel?.Initialize(
			Hierarchy.FilterFilesById(CopyHistorySettings.Items),
			CopyHistorySettings.SelectedItemId);

		_copyHistory = viewModel;
	}

	/// <summary>
	/// Displays the system clipboard.
	/// </summary>
	[RelayCommand]
	private void ShowSystemClipboard()
	{
		if (AppUtils.IsLinux)
		{
			_handler.Watch(TryLaunchLinuxClipboardManagerAsync());

			return;
		}

		_eventSimulator.SimulateKeyPress(KeyCode.VcLeftMeta);

		_eventSimulator.SimulateKeyPress(KeyCode.VcV);

		_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftMeta);

		_eventSimulator.SimulateKeyRelease(KeyCode.VcV);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IDispatcher" />
	protected readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="IExecutionEngine" />
	protected readonly IExecutionEngine _executionEngine;

	/// <inheritdoc cref="IKeyboardInputHook" />
	protected readonly Lazy<IKeyboardInputHook> _keyboardInputHook;

	/// <inheritdoc cref="IAppSettingsManager" />
	protected readonly IAppSettingsManager _settingsManager;

	/// <inheritdoc cref="IViewLauncher" />
	protected readonly IViewLauncher _viewLauncher;

	/// <inheritdoc cref="CopyHistoryViewModel" />
	protected CopyHistoryViewModel? _copyHistory;

	/// <inheritdoc cref="IEventSimulator" />
	private readonly IEventSimulator _eventSimulator;
	#endregion

	#region Constructors
	protected ViewModelBase(
		Application app,
		IAppSettingsManager settingsManager,
		IClipboardService clipboard,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IDispatcher dispatcher,
		IEntityEncryption entityEncryption,
		IEventSimulator eventSimulator,
		IExecutionEngine executionEngine,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler handler,
		IViewLauncher viewLauncher,
		Lazy<IKeyboardInputHook> keyboardInputHook) : base(
			app,
			clipboard,
			dbAccess,
			dialogService,
			entityEncryption,
			logger,
			messenger,
			handler)
	{
		_dispatcher = dispatcher;

		_eventSimulator = eventSimulator;

		_executionEngine = executionEngine;

		_keyboardInputHook = keyboardInputHook;

		_settingsManager = settingsManager;

		_viewLauncher = viewLauncher;

		messenger.RegisterAll(this);

		if (keyboardInputHook.IsValueCreated && keyboardInputHook.Value.IsRunning)
		{
			_handler.Watch(keyboardInputHook.Value.StopTrackingAsync());
		}

		if (settingsManager.Settings.IsDefault() || !settingsManager.Settings.TrackHotkeys)
		{
			return;
		}

		_handler.Watch(keyboardInputHook.Value.StartTrackingAsync(Hierarchy));
	}
	#endregion

	#region Methods
	/// <summary>
	/// Adds objects to <see cref="Hierarchy" />.
	/// </summary>
	public abstract void AddHierarchy(IEnumerable<ExplorerModelBaseDto> hierarchy);

	/// <summary>
	/// Inserts or moves to top value in copy history.
	/// </summary>
	public void InsertToCopyHistory(FileModelDto file, bool updateView)
	{
		if (CopyHistorySettings
			.Items
			.Contains(file.Id))
		{
			CopyHistorySettings
				.Items
				.MoveToTop(CopyHistorySettings.Items.IndexOf(file.Id));
		}
		else
		{
			CopyHistorySettings
				.Items
				.Insert(0, file.Id);
		}

		if (!updateView)
		{
			return;
		}

		_copyHistory?.InsertOrMoveToTop(file);
	}

	/// <inheritdoc />
	public void Receive(CloseExecutingFileMessage message)
	{
		CloseExecutingFile(message.File);
	}

	/// <inheritdoc />
	public void Receive(ShowSnackbarMessage message)
	{
		ShowSnackbar(message.Text, message.Level);
	}

	/// <summary>
	/// Shows the snackbar with <see cref="Brushes.OrangeRed" /> text color.
	/// </summary>
	public void ShowErrorSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Error);

	/// <summary>
	/// Displays object in the "Editor" window.
	/// </summary>
	public abstract Task ShowInEditorAsync(
		Guid id,
		Window window,
		CancellationToken token = default);

	/// <summary>
	/// Shows the snackbar with default text color.
	/// </summary>
	public void ShowInfoSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Information);

	/// <summary>
	/// Shows the snackbar with <see cref="Brushes.Orange" /> text color.
	/// </summary>
	public void ShowWarningSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Warning);

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		if (MessengerHelper.FormatUnsubscriptionLog(this) is { } logLine)
		{
			_logger.LogDebug(logLine);
		}

		_messenger.UnregisterAll(this);
	}

	/// <summary>
	/// Saves data in <see cref="CopyHistorySettings" />.
	/// </summary>
	protected void SaveCopyHistory()
	{
		if (_copyHistory is null)
		{
			return;
		}

		_logger.LogInformation("Save copy history");

		SaveCopyHistory(_copyHistory);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns <c>True</c> if at least one <see cref="SnackbarHost" /> is registered in the application.
	/// </summary>
	private static bool IsSnackbarHostLoaded()
	{
		return typeof(SnackbarHost)
			.GetField("SnackbarHostDictionary", BindingFlags.NonPublic | BindingFlags.Static)
			?.GetValue(null) is IDictionary registered && registered.Count > 0;
	}

	/// <summary>
	/// Tries to open the UI of a known Linux clipboard manager.
	/// Candidates are probed in order; the first one available in <c>PATH</c> wins.
	/// </summary>
	private async Task TryLaunchLinuxClipboardManagerAsync()
	{
		// Each tuple is (executable, args). Order = popularity / desktop coverage.
		(string FileName, string[] Arguments)[] candidates =
		[
			("copyq", ["show"]),
			("gpaste-client", ["ui"]),
			("qdbus", ["org.kde.klipper", "/klipper", "showKlipperPopupMenu"]),
			("xfce4-popup-clipman", []),
			("diodon", []),
		];

		foreach ((string fileName, string[] arguments) in candidates)
		{
			if (await TryStartProcessAsync(fileName, arguments))
			{
				_logger.LogInformation($"System clipboard manager launched: {fileName}");

				return;
			}
		}

		_logger.LogWarning(
			"No supported Linux clipboard manager found in PATH " +
			"(tried copyq, gpaste-client, qdbus/klipper, xfce4-popup-clipman, diodon).");
	}

	/// <summary>
	/// Starts <paramref name="fileName" /> with <paramref name="arguments" />.
	/// Returns <c>true</c> if the executable is available and the call succeeded
	/// (or the UI process is still running after a short grace period).
	/// </summary>
	private static async Task<bool> TryStartProcessAsync(string fileName, string[] arguments)
	{
		try
		{
			ProcessStartInfo psi = new()
			{
				FileName = fileName,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			foreach (string arg in arguments)
			{
				psi.ArgumentList.Add(arg);
			}

			using Process? process = Process.Start(psi);

			if (process is null)
			{
				return false;
			}

			// Short window: short-lived helpers (qdbus) report failure here;
			// long-running UIs (copyq, gpaste-client) are still alive and treated as success.
			using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(500));

			try
			{
				await process.WaitForExitAsync(cts.Token);

				return process.ExitCode == 0;
			}
			catch (OperationCanceledException)
			{
				return true;
			}
		}
		catch (Win32Exception)
		{
			// Executable not present in PATH — try the next candidate.
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <inheritdoc cref="SaveCopyHistory()" />
	private void SaveCopyHistory(CopyHistoryViewModel viewModel)
	{
		if (viewModel.SelectedItem is { } selected)
		{
			CopyHistorySettings.SelectedItemId = selected.Id;
		}
		else
		{
			CopyHistorySettings.SelectedItemId = default;
		}

		Guid[] identifiers = [.. viewModel.GetIdentifiers()];

		foreach (Guid item in CopyHistorySettings
			.Items
			.ToArray())
		{
			if (!identifiers.Contains(item))
			{
				CopyHistorySettings
					.Items
					.Remove(item);
			}
		}
	}

	/// <summary>
	/// Shows the snackbar with default text color.
	/// </summary>
	private void ShowSnackbar(string text, SnackbarMessageLevel level)
	{
		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		_dispatcher.Post(() =>
		{
			if (level == SnackbarMessageLevel.Information)
			{
				SnackbarForeground = _app.GetCurrentTheme() switch
				{
					CurrentTheme.Dark => Brushes.White,
					CurrentTheme.Light => Brushes.Black,
					_ => throw new NotImplementedException()
				};
			}
			else
			{
				SnackbarForeground = level switch
				{
					SnackbarMessageLevel.Warning => Brushes.Orange,
					SnackbarMessageLevel.Error => Brushes.OrangeRed,
					_ => null
				};
			}

			bool isLoaded = IsSnackbarHostLoaded();

			string message = $"{(isLoaded ? "Shown in Snackbar" : "Does not shown in Snackbar")}: {text}";

			switch (level)
			{
				case SnackbarMessageLevel.Warning:
					_logger.LogWarning(message);
					break;

				case SnackbarMessageLevel.Error:
					_logger.LogError(message, isAssertDebug: false);
					break;

				default:
					_logger.LogInformation(message);
					break;
			}

			if (!isLoaded)
			{
				return;
			}

			SnackbarHost.Post(
				new SnackbarModel(text, TimeSpan.FromSeconds(5.0)),
				null,
				DispatcherPriority.Normal);
		});
	}
	#endregion
}
