using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Entities.Abstract;
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
	/// <inheritdoc cref="IDispatcher" />
	protected readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="IKeyboardInputHook" />
	protected readonly IKeyboardInputHook _keyboardInputHook;

	/// <inheritdoc cref="IAppSettingsManager" />
	protected readonly IAppSettingsManager _settingsManager;

	/// <inheritdoc cref="IViewLauncher" />
	protected readonly IViewLauncher _viewLauncher;

	/// <inheritdoc cref="IEventSimulator" />
	private readonly IEventSimulator _eventSimulator;
	#endregion

	#region Constructors
	protected ViewModelBase(
		Application app,
		IAppSettingsManager settingsManager,
		IDbAccess dbAccess,
		IDispatcher dispatcher,
		IEntityEcryption entityEcryption,
		IEventSimulator eventSimulator,
		IKeyboardInputHook keyboardInputHook,
		ILogger logger,
		IViewFactory viewFactory,
		IViewLauncher viewLauncher) : base(app, dbAccess, entityEcryption, logger, viewFactory)
	{
		_dispatcher = dispatcher;

		_eventSimulator = eventSimulator;

		_keyboardInputHook = keyboardInputHook;

		_settingsManager = settingsManager;

		_viewLauncher = viewLauncher;

		if (keyboardInputHook.IsRunning)
		{
			keyboardInputHook.StopTracking();
		}

		if (settingsManager.Settings.IsDefault() || !settingsManager.Settings.TrackHotkeys)
		{
			return;
		}

		_ = keyboardInputHook.StartTrackingAsync(Hierarchy);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Adds objects to <see cref="Hierarchy" />.
	/// </summary>
	public abstract void AddHierarchy(IEnumerable<ExplorerModelBaseDto> hierarchy);

	/// <summary>
	/// Displays "Copy History".
	/// </summary>
	public abstract void DisplayCopyHistory();

	/// <summary>
	/// Saves data in <see cref="CopyHistorySettings" />.
	/// </summary>
	public abstract void SaveCopyHistory();

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
	/// Adds or moves to top value in <see cref="CopyHistoryViewSettings.CopyHistory" />.
	/// </summary>
	public void UpdateCopyHistory(in Guid fileId)
	{
		if (CopyHistorySettings
			.CopyHistory
			.Contains(fileId))
		{
			CopyHistorySettings
				.CopyHistory
				.MoveToTop(CopyHistorySettings.CopyHistory.IndexOf(fileId));
		}
		else
		{
			CopyHistorySettings
				.CopyHistory
				.Insert(0, fileId);
		}
	}

	/// <inheritdoc cref="SaveCopyHistory()" />
	protected void SaveCopyHistory(CopyHistoryViewModel viewModel)
	{
		if (viewModel.SelectedItem is { } selected)
		{
			CopyHistorySettings.SelectedCopyHistoryItemId = selected.Id;
		}
		else
		{
			CopyHistorySettings.SelectedCopyHistoryItemId = default;
		}

		Guid[] identifiers = [.. viewModel.GetIdentifiers()];

		foreach (Guid item in CopyHistorySettings
			.CopyHistory
			.ToArray())
		{
			if (!identifiers.Contains(item))
			{
				CopyHistorySettings
					.CopyHistory
					.Remove(item);
			}
		}

		viewModel.Dispose();
	}
	#endregion

	#region Service
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
