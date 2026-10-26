using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Exchange;
using DataOrganizer.Enums.Views;
using DataOrganizer.Extensions.Hierarchy;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Exchange;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Seeding;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Runtime;
using DataOrganizer.Messages.Updates;
using DataOrganizer.ViewModels.Hierarchy;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DataProtection.Messages;
using Diagnostics.Interfaces;
using DynamicData;
using DynamicData.Binding;
using EditorMessages.Messages;
using Entities.Enums;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.Extensions;
using Notifications.Interfaces;
using Repository.Dto;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Windows;

/// <summary>
/// View model for <c>EditorWindow</c>.
/// </summary>
public sealed partial class EditorViewModel :
	ViewModelBase,
	IRecipient<CloseEditingFileMessage>,
	IRecipient<ConfirmUpdateMessage>,
	IRecipient<SessionAutoLockedMessage>,
	IRecipient<ShowInEditorMessage>,
	IRecipient<ShowProgressBarMessage>
{
	#region Properties
	/// <inheritdoc cref="IAutoLockService" />
	public IAutoLockService AutoLock => _autoLock;

	/// <summary>
	/// The tabs of the files open in the editor.
	/// </summary>
	public EditingFilesViewModel EditingFiles { get; }

	/// <summary>
	/// Information in the lower left corner.
	/// </summary>
	[ObservableProperty]
	public partial string? HierarchySummary { get; set; }

	/// <summary>
	/// Rows to highlight where the view shows them.
	/// </summary>
	public IObservable<ExplorerNodeViewModelBase> HighlightSignal => _highlightSignal;

	/// <summary>
	/// Controls the progress bar for an action.
	/// </summary>
	[ObservableProperty]
	public partial bool IsActionInProgress { get; set; }

	/// <summary>
	/// <c>True</c> when the left drawer should be opened.
	/// </summary>
	[ObservableProperty]
	public partial bool IsLeftDrawerOpened { get; set; }

	/// <inheritdoc cref="ISessionReadOnlyMode.IsReadOnly" />
	public bool IsReadOnly
	{
		get => _sessionReadOnlyMode.IsReadOnly;
		set
		{
			if (!SetProperty(
				_sessionReadOnlyMode.IsReadOnly,
				value,
				_sessionReadOnlyMode,
				static (mode, isReadOnly) => mode.IsReadOnly = isReadOnly))
			{
				return;
			}

			AddCommand.NotifyCanExecuteChanged();

			DeleteCommand.NotifyCanExecuteChanged();

			_messenger.Send(new EditorReadOnlyModeChangedMessage(value));

			_logger.LogDebug(
				$@"""{nameof(IsReadOnly)}"" property of ""{nameof(EditorViewModel)}"" has changed to ""{value}""");
		}
	}

	/// <summary>
	/// <c>True</c> when the right side sheet should be opened.
	/// </summary>
	[ObservableProperty]
	public partial bool IsRightSideSheetOpened { get; set; }

	/// <summary>
	/// <c>True</c> while the window of the editor is loaded and visible.
	/// </summary>
	public bool IsWindowShown { get; set; }

	/// <summary>
	/// Width of the navigation column.
	/// </summary>
	[ObservableProperty]
	public partial double NavigationColumnWidth { get; set; }

	/// <summary>
	/// Rows of the tree that show the objects of <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	public ReadOnlyObservableCollection<ExplorerNodeViewModelBase> Nodes => _nodes;

	/// <inheritdoc cref="RightSideSheetContentKind" />
	[ObservableProperty]
	public partial RightSideSheetContentKind RightSideSheetContent { get; set; }

	/// <summary>
	/// The row selected in <see cref="Nodes" />.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
	[NotifyCanExecuteChangedFor(nameof(ResetSelectedObjectCommand))]
	public partial ExplorerNodeViewModelBase? SelectedNode { get; set; }

	/// <summary>
	/// Paths of the rows for the tree to bring into view, select and highlight, from the top of the tree to the row.
	/// </summary>
	public IObservable<IReadOnlyList<ExplorerNodeViewModelBase>> ShowSignal => _showSignal;

	/// <summary>
	/// The current width of the window.
	/// </summary>
	[ObservableProperty]
	public partial double ViewWidth { get; set; }
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsLeftDrawerOpened" /> changes.
	/// </summary>
	partial void OnIsLeftDrawerOpenedChanged(bool value)
	{
		SeedClipboardHistoryCommand.NotifyCanExecuteChanged();

		SeedLargeSamplesCommand.NotifyCanExecuteChanged();

		SeedSamplesCommand.NotifyCanExecuteChanged();

		ShowFavoritesCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="IsRightSideSheetOpened" /> changes.
	/// </summary>
	partial void OnIsRightSideSheetOpenedChanged(bool value)
	{
		if (value)
		{
			return;
		}

		RightSideSheetContent = RightSideSheetContentKind.None;
	}

	/// <summary>
	/// Called when <see cref="RightSideSheetContent" /> changes.
	/// </summary>
	partial void OnRightSideSheetContentChanged(
		RightSideSheetContentKind oldValue,
		RightSideSheetContentKind newValue)
	{
		if (oldValue == RightSideSheetContentKind.CopyHistory)
		{
			CloseCopyHistoryList();
		}

		if (newValue != RightSideSheetContentKind.CopyHistory)
		{
			return;
		}

		OpenCopyHistoryList();
	}

	/// <summary>
	/// Called when <see cref="SelectedNode" /> changes.
	/// </summary>
	partial void OnSelectedNodeChanging(
		ExplorerNodeViewModelBase? oldValue,
		ExplorerNodeViewModelBase? newValue)
	{
		// The tree clears only the rows it has shown, while one object at most stays selected.
		oldValue?.IsSelected = false;
	}

	/// <summary>
	/// Called when <see cref="ViewWidth" /> changes.
	/// </summary>
	partial void OnViewWidthChanged(double value)
	{
		if (NavigationColumnHelper.FitWidth(NavigationColumnWidth, value) is not { } width)
		{
			return;
		}

		NavigationColumnWidth = width;
	}
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Changes the password of a folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanChangePassword))]
	internal Task ChangePassword(FolderDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		return _protectionScenarios.ChangePasswordAsync(dto);
	}

	/// <summary>
	/// Decrypts the files in a folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanDecryptFolder))]
	internal Task DecryptFolder(FolderDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		return _protectionScenarios.DecryptFolderAsync(dto);
	}

	/// <summary>
	/// Displays the note editing dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanEditNote))]
	internal async Task EditNote(ExplorerItemDtoBase? dto)
	{
		if (dto is null)
		{
			return;
		}

		_logger.LogInformation("Editing a note of an object using dialog");

		TextInputResult result = await _textInputPrompt.RequestMultilineTextAsync(
			_noteReader.ReadNote(dto),
			dto.Name,
			isSensitive: dto.EncryptionStatus != EncryptionStatus.None)
			.ConfigureAwait(false);

		if (!result.IsConfirmed)
		{
			return;
		}

		await _noteEditor
			.EditAsync(dto, result.Value)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Encrypts the files in a folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanEncryptFolder))]
	internal Task EncryptFolder(FolderDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		return _protectionScenarios.EncryptFolderAsync(dto);
	}

	/// <summary>
	/// Executes the file in the operating system.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanBeEditedOrExecuted))]
	internal async Task ExecuteFile(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		if (!await _fileExecution
			.ConfirmAsync(dto)
			.ConfigureAwait(true))
		{
			return;
		}

		if (dto.EncryptionStatus == EncryptionStatus.Encrypted && !await ShowFileContentsAsync(dto).ConfigureAwait(true))
		{
			return;
		}

		await _fileExecution
			.ExecuteAsync(dto, IsReadOnly)
			.ConfigureAwait(true);
	}

	/// <summary>
	/// Hides all file contents.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanHideAllFileContents))]
	internal async Task HideAllFileContents()
	{
		if (!await _protectionScenarios
			.HideAllContentsAsync()
			.ConfigureAwait(true))
		{
			return;
		}

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Hides file contents.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanHideFileContents))]
	internal async Task HideFileContents(FileDto? dto)
	{
		if (dto is null || !await _protectionScenarios
			.HideFileContentsAsync(dto)
			.ConfigureAwait(true))
		{
			return;
		}

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <inheritdoc cref="IProtectionScenarios.HideFolderContentsAsync" />
	[RelayCommand(CanExecute = nameof(CanHideFolderContents))]
	internal async Task HideFolderContents(FolderDto? dto)
	{
		if (dto is null || !await _protectionScenarios
			.HideFolderContentsAsync(dto)
			.ConfigureAwait(true))
		{
			return;
		}

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Imports data.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanImport))]
	internal async Task Import()
	{
		if (await _import
			.ImportAsync(Hierarchy)
			.ConfigureAwait(true) is not { } result)
		{
			return;
		}

		if (result.Variant == ImportMode.Replace)
		{
			IsRightSideSheetOpened = false;
		}

		AddHierarchy(result.Imported.Items);

		_notification.ShowInformationSnackbar(Strings.DataImportCompleted);
	}

	/// <summary>
	/// Resets the <see cref="SelectedNode" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanResetSelectedObject))]
	internal void ResetSelectedObject()
	{
		if (SelectedNode is null)
		{
			return;
		}

		SelectedNode.IsSelected = false;

		SelectedNode = null;
	}

	/// <summary>
	/// Asks the application to start again once it stops.
	/// </summary>
	[RelayCommand]
	internal void RestartApplication() => _messenger.Send(new RestartAppMessage());

	/// <summary>
	/// Sets <see cref="FileDto.IsFavorite" /> value.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSetFavorite))]
	internal async Task SetFavorite(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		dto.IsFavorite = !dto.IsFavorite;

		try
		{
			await _propertyWriter
				.UpdateIsFavoriteAsync(dto)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, Strings.FailedToChangeFavorite);
		}
	}

	/// <summary>
	/// Displays the "Favorites" window.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowFavorites))]
	internal void ShowFavorites()
	{
		CloseCopyHistoryList();

		_sessionEditorTabs.EditorTabs = EditingFiles.State;

		_mainWindowLauncher.SwitchToFavorites();

		Hierarchy
			.GetFilesBy(x => x.IsEditing)
			.ForEach(EditingFiles.CloseEditor);
	}

	/// <summary>
	/// Shows the file contents in a folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowFolderContents))]
	internal async Task ShowFolderContents(FolderDto? dto)
	{
		if (dto is null || !await _protectionScenarios
			.ShowFolderContentsAsync(dto)
			.ConfigureAwait(true))
		{
			return;
		}

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Displays the add object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanAdd))]
	private async Task Add(FolderDto? parent)
	{
		_logger.LogInformation("Adding an object using a dialog");

		if (await _dialogService
			.ShowEntityCreationAsync()
			.ConfigureAwait(false) is not { } result)
		{
			return;
		}

		await AddAsync(
			result.Name,
			result.Type,
			parent).ConfigureAwait(false);
	}

	/// <summary>
	/// Clears current right side sheet content.
	/// </summary>
	[RelayCommand]
	private async Task ClearRightSideSheet()
	{
		if (RightSideSheetContent == RightSideSheetContentKind.None)
		{
			return;
		}

		if (RightSideSheetContent == RightSideSheetContentKind.CopyHistory)
		{
			if (_copyHistory.ItemIds.Count == 0 || !await _confirmationPrompt
				.RequestYesCancelAsync($"{Strings.Clear}?")
				.ConfigureAwait(false))
			{
				return;
			}

			_copyHistoryRecorder.Clear();
		}
		else if (RightSideSheetContent == RightSideSheetContentKind.ExecutingFiles)
		{
			if (ExecutingFiles.Count == 0 || !await _confirmationPrompt
				.RequestYesCancelAsync($"{Strings.Clear}?")
				.ConfigureAwait(false))
			{
				return;
			}

			ExecutingFiles
				.ToArray()
				.ForEach(CloseExecutingFile);
		}
	}

	/// <summary>
	/// Closes a file that is being edited or executed.
	/// </summary>
	[RelayCommand]
	private void CloseOpenedFile(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		_openFilesCloser.Close([dto]);
	}

	/// <summary>
	/// Closes the right side sheet.
	/// </summary>
	[RelayCommand]
	private void CloseRightSideSheet() => IsRightSideSheetOpened = false;

	/// <summary>
	/// Collapses all folders in <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	[RelayCommand]
	private Task CollapseAllFolders() => ExpandCollapseAllFoldersAsync(false);

	/// <inheritdoc cref="IContentCopier.CopyAsync" />
	[RelayCommand(CanExecute = nameof(CanCopyContent))]
	private async Task CopyContentByContextMenu(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		if (!await _contentCopier
			.CopyAsync(dto, showAtOnce: true)
			.ConfigureAwait(true))
		{
			return;
		}

		Highlight(dto);
	}

	/// <summary>
	/// Copies the name of an object to the clipboard.
	/// </summary>
	[RelayCommand]
	private void CopyName(ExplorerItemDtoBase? dto)
	{
		try
		{
			if (dto is null)
			{
				return;
			}

			_exceptionHandler.Watch(_clipboardWriter.WriteTextAsync(dto.Name, isSensitive: false));

			Highlight(dto);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Displays the delete object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanDelete))]
	private async Task Delete(ExplorerItemDtoBase? dto)
	{
		ExplorerItemDtoBase? toBeDeleted = dto ?? SelectedNode?.Model;

		if (toBeDeleted is null)
		{
			return;
		}

		_logger.LogInformation("Deleting an object using a dialog");

		string action = toBeDeleted switch
		{
			FileDto file when file.IsOpened() => Strings.CloseTheFileAndDelete,
			FolderDto folder when folder.Children.ContainsFileBy(IsOpened) => Strings.CloseTheFilesAndDelete,
			_ => Strings.Delete
		};

		if (!await _confirmationPrompt
			.RequestYesNoAsync($@"{action} ""{toBeDeleted.Name}""?")
			.ConfigureAwait(true))
		{
			return;
		}

		// The open files are closed next.
		await DeleteAsync(toBeDeleted).ConfigureAwait(false);
	}

	/// <inheritdoc cref="EditingFilesViewModel.OpenInEditor" />
	[RelayCommand(CanExecute = nameof(CanBeEditedOrExecuted))]
	private async Task EditFile(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		if (dto.EncryptionStatus == EncryptionStatus.Encrypted && !await ShowFileContentsAsync(dto).ConfigureAwait(false))
		{
			return;
		}

		EditingFiles.OpenInEditor(dto);
	}

	/// <summary>
	/// Expands all folders in <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	[RelayCommand]
	private Task ExpandAllFolders() => ExpandCollapseAllFoldersAsync(true);

	/// <summary>
	/// Exports data.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExport))]
	private Task Export() => _dataExchange.ExportDataAsync();

	/// <summary>
	/// Displays the rename object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanRename))]
	private async Task Rename(ExplorerItemDtoBase? dto)
	{
		if (dto is null)
		{
			return;
		}

		_logger.LogInformation("Renaming an object using dialog");

		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.Rename,
			Key = dto.Name,
			KeyHint = Strings.Name
		};

		if (await _textInputPrompt
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } pair)
		{
			return;
		}

		await _hierarchyEditor
			.RenameAsync(dto, pair.Key)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Starts the auto-lock countdown over from the delay currently in the settings.
	/// </summary>
	[RelayCommand]
	private void RestartAutoLock() => _autoLockControl.RestartAutoLock();

	/// <summary>
	/// Adds entries made up for trying the clipboard history to it.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSeedClipboardHistory))]
	private Task SeedClipboardHistory()
	{
		IsLeftDrawerOpened = false;

		return _seeding.SeedClipboardHistoryAsync();
	}

	/// <summary>
	/// Adds a run of many sample objects with large contents to the database and the tree.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSeedSamples))]
	private Task SeedLargeSamples() => SeedAsync(_seeding.SeedLargeSamplesAsync);

	/// <summary>
	/// Adds a run of sample objects to the database and the tree.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSeedSamples))]
	private Task SeedSamples() => SeedAsync(_seeding.SeedSamplesAsync);

	/// <summary>
	/// Selects a row of the tree.
	/// </summary>
	[RelayCommand]
	private void SelectObject(ExplorerNodeViewModelBase? node)
	{
		if (node is null)
		{
			return;
		}

		SelectNode(node);
	}

	/// <summary>
	/// Controls the display of the copy history in right side sheet.
	/// </summary>
	[RelayCommand]
	private void ShowCopyHistory() => SwitchRightSideSheetContent(RightSideSheetContentKind.CopyHistory);

	/// <summary>
	/// Controls the display of the executing files in right side sheet.
	/// </summary>
	[RelayCommand]
	private void ShowExecutingFiles()
	{
		SwitchRightSideSheetContent(RightSideSheetContentKind.ExecutingFiles);
	}

	/// <inheritdoc cref="IProtectionScenarios.ShowFileContentsAsync" />
	[RelayCommand(CanExecute = nameof(CanShowFileContents))]
	private Task ShowFileContents(FileDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		return ShowFileContentsAsync(dto);
	}

	/// <summary>
	/// Displays the hotkey editor.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowHotkeysEditor))]
	private Task ShowHotkeysEditor(FileDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		return _hotkeysEditing.EditAsync(dto, Hierarchy);
	}

	/// <inheritdoc cref="ShowObject" />
	[RelayCommand]
	private void ShowInList(Guid id) => ShowObject(id);

	/// <summary>
	/// Shows a properties view.
	/// </summary>
	[RelayCommand]
	private void ShowProperties(ExplorerItemDtoBase? dto)
	{
		if (dto is null)
		{
			return;
		}

		_dialogService.ShowProperties(GetPropertyDescriptions(dto));
	}

	/// <summary>
	/// Shows application settings.
	/// </summary>
	[RelayCommand]
	private async Task ShowSettings()
	{
		IsLeftDrawerOpened = false;

		_logger.LogInformation("Show settings");

		await _dialogService
			.ShowSettingsAsync()
			.ConfigureAwait(true);

		if (await _settingsCommit
			.EndAsync()
			.ConfigureAwait(true) is not { } saved)
		{
			return;
		}

		IsClipboardHistoryEnabled = saved.TrackClipboardHistory;
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IAutoLockService" />
	private readonly IAutoLockService _autoLock;

	/// <inheritdoc cref="IAutoLockControl" />
	private readonly IAutoLockControl _autoLockControl;

	/// <inheritdoc cref="IClipboardTextWriter" />
	private readonly IClipboardTextWriter _clipboardWriter;

	/// <inheritdoc cref="IConfirmationPrompt" />
	private readonly IConfirmationPrompt _confirmationPrompt;

	/// <inheritdoc cref="IContentCopier" />
	private readonly IContentCopier _contentCopier;

	/// <inheritdoc cref="ICopyHistoryRecorder" />
	private readonly ICopyHistoryRecorder _copyHistoryRecorder;

	/// <inheritdoc cref="IDataExchangeService" />
	private readonly IDataExchangeService _dataExchange;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IDeletionScenario" />
	private readonly IDeletionScenario _deletion;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IFileExecutionScenario" />
	private readonly IFileExecutionScenario _fileExecution;

	/// <inheritdoc cref="IHierarchyEditor" />
	private readonly IHierarchyEditor _hierarchyEditor;

	/// <inheritdoc cref="HighlightSignal" />
	private readonly Subject<ExplorerNodeViewModelBase> _highlightSignal = new();

	/// <inheritdoc cref="IHotkeysEditingScenario" />
	private readonly IHotkeysEditingScenario _hotkeysEditing;

	/// <inheritdoc cref="IImportScenario" />
	private readonly IImportScenario _import;

	/// <inheritdoc cref="IMainWindowLauncher" />
	private readonly IMainWindowLauncher _mainWindowLauncher;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <inheritdoc cref="Nodes" />
	private readonly ReadOnlyObservableCollection<ExplorerNodeViewModelBase> _nodes;

	/// <inheritdoc cref="INoteEditor" />
	private readonly INoteEditor _noteEditor;

	/// <inheritdoc cref="INoteReader" />
	private readonly INoteReader _noteReader;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="IOpenFilesCloser" />
	private readonly IOpenFilesCloser _openFilesCloser;

	/// <inheritdoc cref="IEntityPropertyWriter" />
	private readonly IEntityPropertyWriter _propertyWriter;

	/// <inheritdoc cref="IProtectionScenarios" />
	private readonly IProtectionScenarios _protectionScenarios;

	/// <inheritdoc cref="ISeedingScenarios" />
	private readonly ISeedingScenarios _seeding;

	/// <inheritdoc cref="ISessionEditorTabs" />
	private readonly ISessionEditorTabs _sessionEditorTabs;

	/// <inheritdoc cref="ISessionReadOnlyMode" />
	private readonly ISessionReadOnlyMode _sessionReadOnlyMode;

	/// <inheritdoc cref="ISessionTreeState" />
	private readonly ISessionTreeState _sessionTreeState;

	/// <inheritdoc cref="ISettingsCommit" />
	private readonly ISettingsCommit _settingsCommit;

	/// <inheritdoc cref="ShowSignal" />
	private readonly Subject<IReadOnlyList<ExplorerNodeViewModelBase>> _showSignal = new();
	/// <inheritdoc cref="ITextInputPrompt" />
	private readonly ITextInputPrompt _textInputPrompt;

	/// <summary>
	/// Number of operations showing the progress bar.
	/// </summary>
	private int _actionsInProgress;
	#endregion

	#region Constructors
	public EditorViewModel(
		EditingFilesViewModel editingFiles,
		IAppSettingsStore settingsStore,
		IAutoLockService autoLock,
		IAutoLockControl autoLockControl,
		IClipboardTextWriter clipboardWriter,
		IConfirmationPrompt confirmationPrompt,
		IContentCopier contentCopier,
		ICopyHistory copyHistory,
		ICopyHistoryRecorder copyHistoryRecorder,
		IDataExchangeService dataExchange,
		IDbFailureReporter dbFailureReporter,
		IDeletionScenario deletion,
		IDialogService dialogService,
		IEntityPropertyWriter propertyWriter,
		IExecutingFiles executingFiles,
		IFileExecutionScenario fileExecution,
		IHierarchyEditor hierarchyEditor,
		IHotkeysEditingScenario hotkeysEditing,
		IImportScenario import,
		ILogger logger,
		IMainWindowLauncher mainWindowLauncher,
		IMessenger messenger,
		INoteEditor noteEditor,
		INoteReader noteReader,
		INotificationService notification,
		IOpenFilesCloser openFilesCloser,
		IProtectionScenarios protectionScenarios,
		ISecondaryWindowLauncher secondaryWindowLauncher,
		ISeedingScenarios seeding,
		ISessionEditorTabs sessionEditorTabs,
		ISessionHierarchy sessionHierarchy,
		ISessionReadOnlyMode sessionReadOnlyMode,
		ISessionTreeState sessionTreeState,
		ISettingsCommit settingsCommit,
		ITaskExceptionHandler exceptionHandler,
		ITextInputPrompt textInputPrompt,
		IViewModelFactory viewModelFactory) : base(
			settingsStore,
			copyHistory,
			executingFiles,
			logger,
			messenger,
			secondaryWindowLauncher,
			sessionHierarchy,
			exceptionHandler,
			viewModelFactory)
	{
		EditingFiles = editingFiles;

		_autoLock = autoLock;

		_autoLockControl = autoLockControl;

		_clipboardWriter = clipboardWriter;

		_confirmationPrompt = confirmationPrompt;

		_contentCopier = contentCopier;

		_copyHistoryRecorder = copyHistoryRecorder;

		_dataExchange = dataExchange;

		_dbFailureReporter = dbFailureReporter;

		_deletion = deletion;

		_dialogService = dialogService;

		_fileExecution = fileExecution;

		_hierarchyEditor = hierarchyEditor;

		_hotkeysEditing = hotkeysEditing;

		_import = import;

		_mainWindowLauncher = mainWindowLauncher;

		_noteEditor = noteEditor;

		_noteReader = noteReader;

		_notification = notification;

		_openFilesCloser = openFilesCloser;

		_protectionScenarios = protectionScenarios;

		_propertyWriter = propertyWriter;

		_seeding = seeding;

		_sessionEditorTabs = sessionEditorTabs;

		_sessionReadOnlyMode = sessionReadOnlyMode;

		_sessionTreeState = sessionTreeState;

		_settingsCommit = settingsCommit;

		_textInputPrompt = textInputPrompt;

		Hierarchy
			.ToObservableChangeSet()
			.Transform(x => ExplorerNodeViewModelBase.Create(x, sessionTreeState))
			.DisposeMany()
			.Bind(out _nodes)
			.Subscribe()
			.DisposeWith(_disposables);

		// The expanded folders and the selected object of the tree are kept for the next start.
		Nodes
			.MergeManyInTree(x => x.WhenPropertyChanged(y => y.IsExpanded, notifyOnInitialValue: false))
			.Subscribe(x => SaveIsExpanded(x.Sender.Model.Id, x.Value))
			.DisposeWith(_disposables);

		Nodes
			.MergeManyInTree(x => x.WhenPropertyChanged(y => y.IsSelected, notifyOnInitialValue: false))
			.Subscribe(x => SaveIsSelected(x.Sender.Model, x.Value))
			.DisposeWith(_disposables);

		UpdateHierarchySummary();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Restores the navigation column and the read-only mode from the stored settings.
	/// </summary>
	public void Initialize(EditorWindowSettings windowSettings)
	{
		// A width that was never stored keeps the default one.
		if (windowSettings.NavigationColumnWidth > default(double))
		{
			NavigationColumnWidth = windowSettings.NavigationColumnWidth;
		}

		IsReadOnly = windowSettings.IsReadOnly;
	}

	/// <inheritdoc />
	public void Receive(CloseEditingFileMessage message) => EditingFiles.CloseTab(message.File);

	/// <inheritdoc />
	public void Receive(ConfirmUpdateMessage message)
	{
		message.Reply(_confirmationPrompt.RequestYesNoAsync(message.Text, token: message.Token));
	}

	/// <inheritdoc />
	public void Receive(SessionAutoLockedMessage message) => _exceptionHandler.Watch(LockAsync());

	/// <inheritdoc />
	public void Receive(ShowInEditorMessage message) => ShowObject(message.Id);

	/// <inheritdoc />
	public void Receive(ShowProgressBarMessage message)
	{
		// Operations can nest, so the bar stays until the last of them ends.
		lock (_mutex)
		{
			_actionsInProgress += message.IsVisible ? 1 : -1;

			IsActionInProgress = _actionsInProgress > 0;
		}
	}

	/// <summary>
	/// Selects again the row of the object the tree keeps selected; selecting it again saves nothing.
	/// </summary>
	public void RestoreSelection()
	{
		if (Hierarchy.FindBy(x => _sessionTreeState.IsSelected(x.Id)) is not { } selected
			|| Nodes.FindNode(selected) is not { } node)
		{
			return;
		}

		SelectNode(node);
	}

	/// <summary>
	/// Adds <see cref="ExplorerItemBase" /> to the database and <see cref="ExplorerItemDtoBase" /> to the <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	internal async Task<ExplorerItemDtoBase?> AddAsync(
		string name,
		EntityKind kind,
		FolderDto? parent,
		CancellationToken token = default)
	{
		ExplorerItemDtoBase? dto = await _hierarchyEditor
			.AddAsync(name, kind, parent, Hierarchy, token)
			.ConfigureAwait(false);

		if (dto is not null)
		{
			// The folder opens to show what was added to it.
			if (parent is not null && Nodes.FindNode(parent) is { } folder)
			{
				folder.IsExpanded = true;
			}

			UpdateHierarchySummary();
		}

		return dto;
	}

	/// <summary>
	/// Deletes an object from the database and from <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	internal async Task<bool> DeleteAsync(
		ExplorerItemDtoBase dto,
		CancellationToken token = default)
	{
		if (!await _deletion
			.DeleteAsync(dto, Hierarchy, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		UpdateHierarchySummary();

		return true;
	}

	/// <summary>
	/// Expands or collapses all folders in <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	internal Task ExpandCollapseAllFoldersAsync(bool isExpanded)
	{
		if (!isExpanded)
		{
			ResetSelectedObject();
		}

		ExplorerNodeViewModelBase[] folders =
		[
			.. Hierarchy
				.GetFolders()
				.Select(x => Nodes.FindNode(x))
				.OfType<ExplorerNodeViewModelBase>()
				.Where(x => x.IsExpanded != isExpanded)
		];

		if (folders.IsEmpty())
		{
			return Task.CompletedTask;
		}

		return folders.ForEachAsync(x => x.IsExpanded = isExpanded, Environment.ProcessorCount);
	}

	/// <summary>
	/// Refreshes the command that hides everything and keeps the auto-lock countdown
	/// in step with the decrypted contents.
	/// </summary>
	internal void NotifyDecryptedContentsChanged()
	{
		HideAllFileContentsCommand.NotifyCanExecuteChanged();

		_autoLockControl.RefreshAutoLock();
	}

	/// <summary>
	/// Sets the default <see cref="NavigationColumnWidth" /> for a window <paramref name="windowWidth" /> wide.
	/// </summary>
	internal void SetDefaultNavigationColumnWidth(double windowWidth)
	{
		NavigationColumnWidth = windowWidth / 3.0;
	}

	/// <summary>
	/// Expands the folders of an object and asks the tree to bring the object into view, select and highlight it.
	/// </summary>
	internal void ShowObject(Guid id)
	{
		if (Hierarchy.FindById(id) is not { } found
			|| Nodes.FindPath(found) is not [.. ExplorerNodeViewModelBase[] folders, _] path)
		{
			return;
		}

		foreach (ExplorerNodeViewModelBase folder in folders)
		{
			folder.IsExpanded = true;
		}

		_showSignal.OnNext(path);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="HideFileContentsCommand" />.
	/// </summary>
	private static bool CanHideFileContents(FileDto? dto)
	{
		return dto is not null && dto.EncryptionStatus == EncryptionStatus.Decrypted;
	}

	/// <summary>
	/// Validates <see cref="HideFolderContentsCommand" />.
	/// </summary>
	private static bool CanHideFolderContents(FolderDto? dto)
	{
		return dto?.EncryptionStatus.IsNotDefault() == true
			&& dto.AnyChild(x => x.EncryptionStatus == EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// Returns a sequence with information on the properties of an object.
	/// </summary>
	private static IEnumerable<PropertyDescription> GetPropertyDescriptions(ExplorerItemDtoBase dto)
	{
		const string format = "dd.MM.yyyy HH:mm:ss";

		yield return new(
			Strings.Type,
			dto.Kind switch
			{
				EntityKind.Folder => Strings.Folder,
				EntityKind.File => Strings.File,
				EntityKind.Dataset => Strings.Dataset,
				_ => throw new NotImplementedException()
			});

		yield return new(Strings.Name, dto.Name);

		yield return new(Strings.Created, dto.CreatedAt.ToString(format));

		yield return new(Strings.Updated, dto.UpdatedAt.ToString(format));
	}

	/// <inheritdoc cref="FileDto.IsOpened" />
	private static bool IsOpened(FileDto dto) => dto.IsOpened();

	/// <summary>
	/// Adds objects to <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	private void AddHierarchy(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		Hierarchy.AddRange(hierarchy);

		UpdateHierarchySummary();
	}

	/// <summary>
	/// Validates <see cref="AddCommand" />.
	/// </summary>
	private bool CanAdd() => !IsReadOnly && !IsActionInProgress;

	/// <summary>
	/// <c>True</c> when file can be edited or executed.
	/// </summary>
	private bool CanBeEditedOrExecuted(FileDto? dto)
	{
		return !IsActionInProgress
			&& dto is not null
			&& !IsOpened(dto);
	}

	/// <summary>
	/// Validates <see cref="ChangePasswordCommand" />.
	/// </summary>
	private bool CanChangePassword(FolderDto? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto is not null
			&& FolderProtectionRules.CanChangePassword(dto);
	}

	/// <summary>
	/// Validates <see cref="CopyContentByContextMenuCommand" />.
	/// </summary>
	private bool CanCopyContent(FileDto? dto) => dto?.IsOpened() == false && !IsActionInProgress;

	/// <summary>
	/// Validates <see cref="DecryptFolderCommand" />.
	/// </summary>
	private bool CanDecryptFolder(FolderDto? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto is not null
			&& FolderProtectionRules.CanDecrypt(dto);
	}

	/// <summary>
	/// Validates <see cref="DeleteCommand" />.
	/// </summary>
	private bool CanDelete(ExplorerItemDtoBase? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& (dto is not null || SelectedNode is not null);
	}

	/// <summary>
	/// Validates <see cref="EditNoteCommand" />.
	/// </summary>
	private bool CanEditNote(ExplorerItemDtoBase? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto is not null
			&& dto.EncryptionStatus != EncryptionStatus.Encrypted;
	}

	/// <summary>
	/// Validates <see cref="EncryptFolderCommand" />.
	/// </summary>
	private bool CanEncryptFolder(FolderDto? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto is not null
			&& FolderProtectionRules.CanEncrypt(dto);
	}

	/// <summary>
	/// Validates <see cref="ExportCommand" />.
	/// </summary>
	private bool CanExport() => !IsActionInProgress && Hierarchy.Count > 0;

	/// <summary>
	/// Validates <see cref="HideAllFileContentsCommand" />.
	/// </summary>
	private bool CanHideAllFileContents() => Hierarchy.ContainsBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted);

	/// <summary>
	/// Validates <see cref="ImportCommand" />.
	/// </summary>
	private bool CanImport() => !IsReadOnly && !IsActionInProgress;

	/// <summary>
	/// Validates <see cref="RenameCommand" />.
	/// </summary>
	private bool CanRename(ExplorerItemDtoBase? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto is not null
			&& (dto is not FileDto file || !file.IsOpened());
	}

	/// <summary>
	/// Validates <see cref="ResetSelectedObjectCommand" />.
	/// </summary>
	private bool CanResetSelectedObject() => SelectedNode is not null;

	/// <summary>
	/// Validates <see cref="SeedClipboardHistoryCommand" />.
	/// </summary>
	private bool CanSeedClipboardHistory() => IsClipboardHistoryEnabled;

	/// <summary>
	/// Validates <see cref="SeedLargeSamplesCommand" /> and <see cref="SeedSamplesCommand" />.
	/// </summary>
	private bool CanSeedSamples() => !IsReadOnly && !IsActionInProgress;

	/// <summary>
	/// Validates <see cref="SetFavoriteCommand" />.
	/// </summary>
	private bool CanSetFavorite() => !IsReadOnly && !IsActionInProgress;

	/// <summary>
	/// Validates <see cref="ShowFavoritesCommand" />.
	/// </summary>
	private bool CanShowFavorites() => !IsActionInProgress && Hierarchy.ContainsFileBy(x => x.IsFavorite);

	/// <summary>
	/// Validates <see cref="ShowFileContentsCommand" />.
	/// </summary>
	private bool CanShowFileContents(FileDto? dto)
	{
		return !IsActionInProgress
			&& dto is not null
			&& dto.EncryptionStatus == EncryptionStatus.Encrypted;
	}

	/// <summary>
	/// Validates <see cref="ShowFileContentsCommand" />.
	/// </summary>
	private bool CanShowFolderContents(FolderDto? dto)
	{
		return !IsActionInProgress
			&& dto is not null
			&& dto.EncryptionStatus != EncryptionStatus.None
			&& dto.AnyChild(x => x.EncryptionStatus == EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// Validates <see cref="ShowHotkeysEditorCommand" />.
	/// </summary>
	private bool CanShowHotkeysEditor() => !IsReadOnly && !IsActionInProgress;

	/// <summary>
	/// Asks the view to highlight the row of an object where it shows it.
	/// </summary>
	private void Highlight(ExplorerItemDtoBase dto)
	{
		if (Nodes.FindNode(dto) is not { } node)
		{
			return;
		}

		_highlightSignal.OnNext(node);
	}

	/// <summary>
	/// Hides the decrypted contents once the auto-lock expires and refreshes the command that hides them all.
	/// </summary>
	private async Task LockAsync()
	{
		await _protectionScenarios
			.LockAsync()
			.ConfigureAwait(true);

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Saves a folder of the tree expanded or collapsed, unless the editor is read-only or busy with an action.
	/// </summary>
	private void SaveIsExpanded(Guid folderId, bool isExpanded)
	{
		if (IsReadOnly || IsActionInProgress)
		{
			return;
		}

		_exceptionHandler.Watch(_propertyWriter.UpdateIsExpandedAsync(folderId, isExpanded));
	}

	/// <summary>
	/// Saves an object of the tree selected or cleared, unless the editor is read-only, busy with an action or not shown.
	/// </summary>
	private void SaveIsSelected(ExplorerItemDtoBase item, bool isSelected)
	{
		if (IsReadOnly
			|| IsActionInProgress
			|| !IsWindowShown)
		{
			return;
		}

		_exceptionHandler.Watch(_propertyWriter.UpdateIsSelectedAsync(item, isSelected));
	}

	/// <summary>
	/// Adds to the tree the run of samples that <paramref name="seed" /> makes, with the menu closed, and counts the objects
	/// again.
	/// </summary>
	private async Task SeedAsync(Func<Collection<ExplorerItemDtoBase>, Task> seed)
	{
		IsLeftDrawerOpened = false;

		await seed(Hierarchy).ConfigureAwait(true);

		UpdateHierarchySummary();
	}

	/// <summary>
	/// Marks a row selected and makes it the <see cref="SelectedNode" />.
	/// </summary>
	private void SelectNode(ExplorerNodeViewModelBase node)
	{
		node.IsSelected = true;

		SelectedNode = node;
	}

	/// <inheritdoc cref="IProtectionScenarios.ShowFileContentsAsync" />
	private async Task<bool> ShowFileContentsAsync(FileDto dto)
	{
		if (!await _protectionScenarios
			.ShowFileContentsAsync(dto)
			.ConfigureAwait(true))
		{
			return false;
		}

		HideAllFileContentsCommand.NotifyCanExecuteChanged();

		return true;
	}

	/// <summary>
	/// Switches the right side sheet content.
	/// </summary>
	private void SwitchRightSideSheetContent(RightSideSheetContentKind content)
	{
		if (RightSideSheetContent == content)
		{
			IsRightSideSheetOpened = false;

			return;
		}

		_logger.LogInformation($"Show {content switch
		{
			RightSideSheetContentKind.CopyHistory => "copy history",
			RightSideSheetContentKind.ExecutingFiles => "executing files",
			_ => "unknown"
		}}");

		RightSideSheetContent = content;

		IsRightSideSheetOpened = true;
	}

	/// <summary>
	/// Counts the number of objects in <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	private void UpdateHierarchySummary() => HierarchySummary = Hierarchy.GetCount().AsString();
	#endregion
}
