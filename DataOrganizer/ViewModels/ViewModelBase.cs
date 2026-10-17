using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Diagnostics;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Encryption;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// Base view model.
/// </summary>
public abstract partial class ViewModelBase :
	ObservableDisposableBase,
	IRecipient<SessionAutoLockedMessage>
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

	/// <summary>
	/// <c>True</c> when initialization has completed.
	/// </summary>
	public bool IsInitialized { get; protected set; }

	/// <summary>
	/// <c>True</c> when the window closes to switch to the other main window.
	/// </summary>
	public bool IsSwitchingWindow { get; protected set; }
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
	/// Brings the open notepad to the center of the screen of the window.
	/// </summary>
	[RelayCommand]
	private void CenterNotepad(Window? owner)
	{
		if (owner is null)
		{
			return;
		}

		_secondaryWindowLauncher.CenterNotepadWindow(owner);
	}

	/// <summary>
	/// Opens the custom clipboard history overlay.
	/// </summary>
	[RelayCommand]
	private Task ShowCustomClipboard(Window? owner)
	{
		if (owner is null)
		{
			return Task.CompletedTask;
		}

		return _secondaryWindowLauncher.ShowClipboardLogWindowAsync(owner);
	}

	/// <summary>
	/// Activates the open notepad or opens a new one.
	/// </summary>
	[RelayCommand]
	private void ShowNotepad(Window? owner)
	{
		if (owner is null)
		{
			return;
		}

		_secondaryWindowLauncher.ShowNotepadWindow(owner);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="Application" />
	protected readonly Application _app;

	/// <inheritdoc cref="IContentVisibility" />
	protected readonly IContentVisibility _contentVisibility;

	/// <inheritdoc cref="ICopyHistory" />
	protected readonly ICopyHistory _copyHistory;

	/// <inheritdoc cref="IDialogService" />
	protected readonly IDialogService _dialogService;

	/// <inheritdoc cref="IDispatcherAccessor" />
	protected readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IExecutingFiles" />
	protected readonly IExecutingFiles _executingFiles;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	protected readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	protected readonly INotificationService _notification;

	/// <inheritdoc cref="ISecondaryWindowLauncher" />
	private readonly ISecondaryWindowLauncher _secondaryWindowLauncher;

	/// <inheritdoc cref="ISessionEditorTabs" />
	protected readonly ISessionEditorTabs _sessionEditorTabs;

	/// <inheritdoc cref="ISessionHierarchy" />
	private readonly ISessionHierarchy _sessionHierarchy;

	/// <inheritdoc cref="IAppSettingsStore" />
	protected readonly IAppSettingsStore _settingsStore;

	/// <inheritdoc cref="IViewFactory" />
	protected readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	protected ViewModelBase(
		Application app,
		IAppSettingsStore settingsStore,
		IContentVisibility contentVisibility,
		ICopyHistory copyHistory,
		IDialogService dialogService,
		IDispatcherAccessor dispatcher,
		IExecutingFiles executingFiles,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ISecondaryWindowLauncher secondaryWindowLauncher,
		ISessionEditorTabs sessionEditorTabs,
		ISessionHierarchy sessionHierarchy,
		ITaskExceptionHandler exceptionHandler,
		IViewFactory viewFactory)
	{
		_app = app;

		_contentVisibility = contentVisibility;

		_copyHistory = copyHistory;

		_dialogService = dialogService;

		_dispatcher = dispatcher;

		_exceptionHandler = exceptionHandler;

		_executingFiles = executingFiles;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

		_secondaryWindowLauncher = secondaryWindowLauncher;

		_sessionEditorTabs = sessionEditorTabs;

		_sessionHierarchy = sessionHierarchy;

		_settingsStore = settingsStore;

		_viewFactory = viewFactory;

		IsClipboardHistoryEnabled = settingsStore.Settings is { TrackClipboardHistory: true };

		messenger.RegisterAll(this);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Receive(SessionAutoLockedMessage message)
	{
		_exceptionHandler.Watch(LockAsync());
	}

	/// <summary>
	/// Displays object in the "Editor" window.
	/// </summary>
	public abstract Task ShowInEditorAsync(
		Guid id,
		Window window,
		CancellationToken token = default);

	/// <summary>
	/// Closes editing and executing files.
	/// </summary>
	internal void CloseFiles(
		IEnumerable<FileDto> editingFiles,
		IEnumerable<FileDto> executingFiles)
	{
		foreach (FileDto file in editingFiles)
		{
			CloseEditingFile(file);
		}

		foreach (FileDto file in executingFiles)
		{
			CloseExecutingFile(file);
		}
	}

	/// <summary>
	/// Reacts to a change of the decrypted contents in <see cref="Hierarchy" />.
	/// </summary>
	protected internal virtual void NotifyDecryptedContentsChanged()
	{
	}

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
	/// Marks the file as no longer being edited.
	/// </summary>
	protected virtual void CloseEditingFile(FileDto file) => file.IsEditing = false;

	/// <summary>
	/// Closes file that is being edited or executed;
	/// </summary>
	protected void CloseFile(FileDto file)
	{
		if (file.IsEditing)
		{
			CloseEditingFile(file);
		}

		if (file.IsExecuting)
		{
			CloseExecutingFile(file);
		}
	}

	/// <summary>
	/// Creates the list of the copy history that the window starts to show, read afresh from the history.
	/// </summary>
	protected void OpenCopyHistoryList() => CopyHistoryList = _viewFactory.CreateViewModel<CopyHistoryViewModel>();
	#endregion

	#region Helpers
	/// <summary>
	/// Hides the decrypted contents after the auto-lock expiry, closing the open files first.
	/// </summary>
	private async Task LockAsync()
	{
		_logger.LogInformation("Auto-lock: hiding the decrypted contents");

		// Unlike hiding by hand, the lock is not called off by an editor that failed to persist its changes.
		await _messenger.FlushEditorsAsync().ConfigureAwait(true);

		FileDto[] openedFiles = [.. Hierarchy.GetFilesBy(static x => x.IsOpened())];

		CloseFiles(
			openedFiles.Where(x => x.IsEditing),
			openedFiles.Where(x => x.IsExecuting));

		_sessionEditorTabs.EditorTabs = null;

		_contentVisibility.HideAllContents(Hierarchy);

		NotifyDecryptedContentsChanged();

		_notification.ShowInformationSnackbar(Strings.ContentsHiddenByAutoLock);
	}
	#endregion
}
