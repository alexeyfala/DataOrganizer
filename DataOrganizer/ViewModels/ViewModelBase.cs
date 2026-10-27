using AppInstance.Interfaces;
using Clipboard.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Helpers.Diagnostics;
using DataOrganizer.Interfaces.Views;
using Diagnostics.Interfaces;
using FileExecution.Interfaces;
using Hierarchy.Dto;
using Serilog;
using Settings.Interfaces;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WindowContracts.Interfaces;

namespace DataOrganizer.ViewModels;

/// <summary>
/// Base view model.
/// </summary>
public abstract partial class ViewModelBase : ObservableDisposableBase
{
	#region Properties
	/// <inheritdoc cref="ICopyHistory.ItemIds" />
	public ReadOnlyObservableCollection<Guid> CopyHistoryItemIds => _copyHistory.ItemIds;

	/// <summary>
	/// The list of the copy history while the window shows it.
	/// </summary>
	[ObservableProperty]
	public partial CopyHistoryViewModel? CopyHistoryList { get; private set; }

	/// <inheritdoc cref="IExecutingFiles.Files" />
	public ReadOnlyObservableCollection<FileDto> ExecutingFiles => _executingFiles.Files;

	/// <inheritdoc cref="ISessionHierarchy.Hierarchy" />
	public ObservableCollection<ExplorerItemDtoBase> Hierarchy => _sessionHierarchy.Hierarchy;

	/// <summary>
	/// <c>True</c> when clipboard history tracking is enabled in settings.
	/// </summary>
	[ObservableProperty]
	public partial bool IsClipboardHistoryEnabled { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes a file executing in the operating system.
	/// </summary>
	[RelayCommand]
	internal void CloseExecutingFile(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		_executingFiles.Close(dto);
	}

	/// <summary>
	/// Brings the open notepad to the center of the screen of the main window.
	/// </summary>
	[RelayCommand]
	private void CenterNotepad() => _secondaryWindowLauncher.CenterNotepadWindow();

	/// <summary>
	/// Opens the custom clipboard history overlay.
	/// </summary>
	[RelayCommand]
	private Task ShowCustomClipboard() => _secondaryWindowLauncher.ShowClipboardLogWindowAsync();

	/// <summary>
	/// Activates the open notepad or opens a new one.
	/// </summary>
	[RelayCommand]
	private void ShowNotepad() => _secondaryWindowLauncher.ShowNotepadWindow();
	#endregion

	#region Data
	/// <inheritdoc cref="ICopyHistory" />
	protected readonly ICopyHistory _copyHistory;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IExecutingFiles" />
	protected readonly IExecutingFiles _executingFiles;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	protected readonly IMessenger _messenger;

	/// <inheritdoc cref="ISecondaryWindowLauncher" />
	private readonly ISecondaryWindowLauncher _secondaryWindowLauncher;

	/// <inheritdoc cref="ISessionHierarchy" />
	private readonly ISessionHierarchy _sessionHierarchy;

	/// <inheritdoc cref="IViewModelFactory" />
	protected readonly IViewModelFactory _viewModelFactory;
	#endregion

	#region Constructors
	protected ViewModelBase(
		IAppSettingsStore settingsStore,
		ICopyHistory copyHistory,
		IExecutingFiles executingFiles,
		ILogger logger,
		IMessenger messenger,
		ISecondaryWindowLauncher secondaryWindowLauncher,
		ISessionHierarchy sessionHierarchy,
		ITaskExceptionHandler exceptionHandler,
		IViewModelFactory viewModelFactory)
	{
		_copyHistory = copyHistory;

		_exceptionHandler = exceptionHandler;

		_executingFiles = executingFiles;

		_logger = logger;

		_messenger = messenger;

		_secondaryWindowLauncher = secondaryWindowLauncher;

		_sessionHierarchy = sessionHierarchy;

		_viewModelFactory = viewModelFactory;

		IsClipboardHistoryEnabled = settingsStore.Settings is { TrackClipboardHistory: true };

		messenger.RegisterAll(this);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		if (MessengerSubscriptions.FormatUnsubscriptionLog(this) is { } logLine)
		{
			_logger.LogDebug(logLine);
		}

		_messenger.UnregisterAll(this);

		CloseCopyHistoryList();
	}

	/// <summary>
	/// Disposes the list of the copy history that the window no longer shows.
	/// </summary>
	protected void CloseCopyHistoryList()
	{
		CopyHistoryList?.Dispose();

		CopyHistoryList = null;
	}

	/// <summary>
	/// Creates the list of the copy history that the window starts to show, read afresh from the history.
	/// </summary>
	protected void OpenCopyHistoryList() => CopyHistoryList = _viewModelFactory.CreateViewModel<CopyHistoryViewModel>();
	#endregion
}
