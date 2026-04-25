using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using Material.Styles.Controls;
using Material.Styles.Models;
using Repository.Interfaces;
using Serilog;
using Serilog.Events;
using Shared.Extensions;
using SharpHook;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

/// <summary>
/// Base view model.
/// </summary>
public abstract partial class ViewModelBase : CopyContentViewModelBase
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
	/// Returns <c>True</c> if initialization has completed.
	/// </summary>
	public bool IsInitialized { get; protected set; }

	/// <summary>
	/// Returns <c>True</c> if shutdown is requested.
	/// </summary>
	public bool IsShutdown { get; protected set; } = true;

	/// <summary>
	/// Opened in editor files.
	/// </summary>
	public List<FileModelDto> OpenedInEditorFiles { get; } = [];
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Snackbar's text color.
	/// </summary>
	[ObservableProperty]
	private IBrush? _snackbarForeground;
	#endregion

	#region Auto-Generated Commands
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
		_eventSimulator.SimulateKeyPress(KeyCode.VcLeftMeta);

		_eventSimulator.SimulateKeyPress(KeyCode.VcV);

		_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftMeta);

		_eventSimulator.SimulateKeyRelease(KeyCode.VcV);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IKeyboardInputHook" />
	protected readonly IKeyboardInputHook _keyboardInputHook;

	/// <inheritdoc cref="IAppSettingsManager" />
	protected readonly IAppSettingsManager _settingsManager;

	/// <inheritdoc cref="IViewLauncher" />
	protected readonly IViewLauncher _viewLauncher;

	/// <inheritdoc cref="CopyHistoryViewModel" />
	protected CopyHistoryViewModel? _copyHistory;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

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
		IEntityEcryption entityEcryption,
		IEventSimulator eventSimulator,
		IKeyboardInputHook keyboardInputHook,
		ILogger logger,
		ITaskExceptionHandler handler,
		IViewLauncher viewLauncher,
		IViewModelExecutionService viewModel) : base(
			app,
			clipboard,
			dbAccess,
			dialogService,
			entityEcryption,
			logger,
			handler,
			viewModel)
	{
		_dispatcher = dispatcher;

		_eventSimulator = eventSimulator;

		_keyboardInputHook = keyboardInputHook;

		_settingsManager = settingsManager;

		_viewLauncher = viewLauncher;

		if (keyboardInputHook.IsRunning)
		{
			_handler.Watch(keyboardInputHook.StopTrackingAsync());
		}

		if (settingsManager.Settings.IsDefault() || !settingsManager.Settings.TrackHotkeys)
		{
			return;
		}

		_handler.Watch(keyboardInputHook.StartTrackingAsync(Hierarchy));
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
	public void InsertToCopyHistory(FileModelDto file, in bool updateView)
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

	/// <summary>
	/// Shows the snackbar with <see cref="Brushes.OrangeRed" /> text color.
	/// </summary>
	public void ShowErrorSnackbar(string text) => ShowSnackbar(text, LogEventLevel.Error);

	/// <summary>
	/// Displays object in the "Editor" window.
	/// </summary>
	public abstract Task ShowInEditorAsync(
		Window? window,
		Guid id,
		CancellationToken token = default);

	/// <summary>
	/// Shows the snackbar with default text color.
	/// </summary>
	public void ShowInfoSnackbar(string text) => ShowSnackbar(text);

	/// <summary>
	/// Shows the snackbar with <see cref="Brushes.Orange" /> text color.
	/// </summary>
	public void ShowWarningSnackbar(string text) => ShowSnackbar(text, LogEventLevel.Warning);

	/// <summary>
	/// Clears copy history.
	/// </summary>
	protected void ClearCopyHistory()
	{
		_copyHistory?.Clear();

		SaveCopyHistory();
	}

	/// <summary>
	/// Tries to remove value from copy history.
	/// </summary>
	protected void RemoveFromCopyHistory(FileModelDto file)
	{
		CopyHistorySettings
			.Items
			.Remove(file.Id);

		_copyHistory?.Remove(file);
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

	#region Service
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
	private void ShowSnackbar(string text, LogEventLevel? level = null)
	{
		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		_dispatcher.Post(() =>
		{
			if (level is null)
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
					LogEventLevel.Warning => Brushes.Orange,
					LogEventLevel.Error => Brushes.OrangeRed,
					_ => null
				};
			}

			string message = $"Shown in Snackbar: {text}";

			switch (level)
			{
				case LogEventLevel.Warning:
					_logger.LogWarning(message);
					break;

				case LogEventLevel.Error:
					_logger.LogError(message, isAssertDebug: false);
					break;

				default:
					_logger.LogInformation(message);
					break;
			}

			SnackbarHost.Post(
				new SnackbarModel(text, TimeSpan.FromSeconds(5.0)),
				null,
				DispatcherPriority.Normal);
		});
	}
	#endregion
}
