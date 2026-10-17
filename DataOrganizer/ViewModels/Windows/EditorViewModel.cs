using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Enums.Views;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Execution;
using DataOrganizer.Helpers.Hierarchy;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Messages.Runtime;
using DataOrganizer.Messages.Updates;
using DataOrganizer.Windows;
using DynamicData.Binding;
using Entities.Enums;
using Entities.Models;
using Material.Styles.Controls;
using Repository.Dto;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
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
	/// Objects to highlight where the view shows them.
	/// </summary>
	public IObservable<ExplorerItemDtoBase> HighlightSignal => _highlightSignal;

	/// <summary>
	/// Controls the progress bar for an action.
	/// </summary>
	[ObservableProperty]
	public partial bool IsActionInProgress { get; set; }

	/// <summary>
	/// Controls the display of the <see cref="NavigationDrawer" />.
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
	/// Width of the navigation column.
	/// </summary>
	[ObservableProperty]
	public partial GridLength NavigationColumnWidth { get; set; }

	/// <inheritdoc cref="RightSideSheetContentKind" />
	[ObservableProperty]
	public partial RightSideSheetContentKind RightSideSheetContent { get; set; }

	/// <summary>
	/// The selected object in <see cref="TreeView" /> from <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
	[NotifyCanExecuteChangedFor(nameof(ResetSelectedObjectCommand))]
	public partial ExplorerItemDtoBase? SelectedObject { get; set; }

	/// <summary>
	/// Objects for the tree to bring into view, select and highlight.
	/// </summary>
	public IObservable<ExplorerItemDtoBase> ShowSignal => _showSignal;

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
	/// Called when <see cref="SelectedObject" /> changes.
	/// </summary>
	partial void OnSelectedObjectChanging(
		ExplorerItemDtoBase? oldValue,
		ExplorerItemDtoBase? newValue)
	{
		// The tree clears only the rows it has shown, while one object at most stays selected.
		oldValue?.IsSelected = false;
	}

	/// <summary>
	/// Called when <see cref="ViewWidth" /> changes.
	/// </summary>
	partial void OnViewWidthChanged(double value)
	{
		if (NavigationColumnHelper.FitWidth(NavigationColumnWidth.Value, value) is not { } width)
		{
			return;
		}

		NavigationColumnWidth = new GridLength(width);
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

		TextInputResult result = await _dialogService.RequestMultilineTextAsync(
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

		if (_executingFiles.IsExecuting(dto.Id))
		{
			_logger.LogWarning($"The file is already executing in the operating system:{dto.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Name),
				nameof(FileDto.Kind))}");

			return;
		}

		if (ExecutableFileDetector.IsExecutable(dto.Name))
		{
			string text =
				$"{string.Format(Strings.TheFileMayRunProgramOrScriptOnYourDevice, dto.Name, Environment.NewLine)}" +
				Environment.NewLine +
				Environment.NewLine +
				$"{Strings.OpenTheExecutableFile}?";

			if (!await _dialogService
				.RequestYesCancelAsync(text)
				.ConfigureAwait(true))
			{
				return;
			}
		}

		_logger.LogInformation($"The file needs to be executed in the operating system:{dto.GetPropertyValues(
			true,
			nameof(FileDto.Id),
			nameof(FileDto.Name),
			nameof(FileDto.Kind))}");

		if (dto.EncryptionStatus == EncryptionStatus.Encrypted && !await ShowFileContentsAsync(dto).ConfigureAwait(true))
		{
			return;
		}

		// The key that opens the contents also encrypts the changes made to them while the file runs.
		Guid? keeperId = dto.FindUnlockedKeeper()?.Id;

		FileContentsReading reading;

		try
		{
			reading = await _fileContentsReader
				.ReadUnlockedAsync(dto.Id, keeperId)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			_notification.ShowErrorSnackbar($@"{Strings.FailedToLoadFileContents} ""{dto.Name}""");

			return;
		}

		if (reading.Status == FileContentsStatus.Damaged)
		{
			string errorText = $@"{Strings.FailedToLoadFileContents} ""{dto.Name}""";

			_notification.ShowErrorSnackbar(errorText);

			_logger.LogError($"{errorText}:{dto.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Kind))}");

			return;
		}

		if (reading.Status == FileContentsStatus.Closed)
		{
			_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

			return;
		}

		_logger.LogInformation(
			$"Contents of the {reading.Contents.Length}-byte file loaded from the database:{dto.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Name),
				nameof(FileDto.Kind),
				nameof(FileDto.UpdatedAt))}");

		ExecuteFileParameters parameters = new()
		{
			Contents = reading.Contents,
			File = dto,
			IsReadOnly = IsReadOnly,
			KeeperId = keeperId,
		};

		await _executingFiles
			.ExecuteAsync(parameters)
			.ConfigureAwait(true);
	}

	/// <summary>
	/// Exits the application.
	/// </summary>
	[RelayCommand]
	internal void Exit(Window? window)
	{
		window?.Close();

		_messenger.Send(new ExitAppMessage());
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
		if (!await _openFilesCloser
			.TryCloseAsync([.. Hierarchy.GetFilesBy(IsOpened)])
			.ConfigureAwait(true))
		{
			return;
		}

		if (await _dataExchange
			.ImportDataAsync(Hierarchy)
			.ConfigureAwait(true) is not { } result || result.Variant == ImportMode.None)
		{
			return;
		}

		if (result.Variant == ImportMode.Replace)
		{
			Hierarchy.Clear();

			_copyHistory.Clear();

			IsRightSideSheetOpened = false;

			_contentVisibility.DiscardAllKeys();
		}

		AddHierarchy(result.ImportedItems);

		_notification.ShowInformationSnackbar(Strings.DataImportCompleted);
	}

	/// <summary>
	/// Resets the <see cref="SelectedObject" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanResetSelectedObject))]
	internal void ResetSelectedObject()
	{
		if (SelectedObject is null)
		{
			return;
		}

		SelectedObject.IsSelected = false;

		SelectedObject = null;
	}

	/// <summary>
	/// Restarts the application.
	/// </summary>
	[RelayCommand]
	internal void RestartApplication(Window? window)
	{
		Exit(window);

		if (Environment.ProcessPath is null)
		{
			return;
		}

		_processManager.StartProcess(Environment.ProcessPath);
	}

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
	internal void ShowFavorites(EditorWindow? window)
	{
		window?.Close();

		CloseCopyHistoryList();

		_sessionEditorTabs.EditorTabs = EditingFiles.State;

		_mainWindowLauncher
			.CreateFavoritesWindow()
			.Show();

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
			if (_copyHistory.ItemIds.Count == 0 || !await _dialogService
				.RequestYesCancelAsync($"{Strings.Clear}?")
				.ConfigureAwait(false))
			{
				return;
			}

			_copyHistory.Clear();
		}
		else if (RightSideSheetContent == RightSideSheetContentKind.ExecutingFiles)
		{
			if (ExecutingFiles.Count == 0 || !await _dialogService
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

	/// <inheritdoc cref="ViewModelBase.CloseFile" />
	[RelayCommand]
	private void CloseOpenedFile(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		CloseFile(dto);
	}

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
		ExplorerItemDtoBase? toBeDeleted = dto ?? SelectedObject;

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

		if (!await _dialogService
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
		ExplorerItemDtoBase? toBeRenamed = dto ?? SelectedObject;

		if (toBeRenamed is null)
		{
			return;
		}

		_logger.LogInformation("Renaming an object using dialog");

		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.Rename,
			Key = toBeRenamed.Name,
			KeyHint = Strings.Name
		};

		if (await _dialogService
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } pair)
		{
			return;
		}

		await _hierarchyEditor
			.RenameAsync(toBeRenamed, pair.Key)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Starts the auto-lock countdown over from the delay currently in the settings.
	/// </summary>
	[RelayCommand]
	private void RestartAutoLock() => _backgroundServiceControl.RestartAutoLock();

	/// <summary>
	/// Adds entries made up for trying the clipboard history to it.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSeedClipboardHistory))]
	private async Task SeedClipboardHistory()
	{
		IsLeftDrawerOpened = false;

		_logger.LogInformation("Seed clipboard history");

		try
		{
			await _clipboardLogSeeder
				.SeedAsync()
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException("The sample clipboard entries could not be added.", ex);
		}
	}

	/// <summary>
	/// Adds a run of many sample objects with large contents to the database and the tree.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSeedSamples))]
	private Task SeedLargeSamples() => SeedAsync("Seed large samples", _sampleSeeder.SeedLargeAsync);

	/// <summary>
	/// Adds a run of sample objects to the database and the tree.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSeedSamples))]
	private Task SeedSamples() => SeedAsync("Seed samples", _sampleSeeder.SeedAsync);

	/// <summary>
	/// Selects an object of the tree.
	/// </summary>
	[RelayCommand]
	private void SelectObject(ExplorerItemDtoBase? dto)
	{
		if (dto is null)
		{
			return;
		}

		SetSelectedObject(dto);
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
	private async Task ShowHotkeysEditor(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		_logger.LogInformation("Show hotkeys editor");

		await _backgroundServiceControl
			.PauseHotkeyTrackingAsync()
			.ConfigureAwait(true);

		EditingHotkeysResult result = await _dialogService
			.EditHotkeysAsync(dto.Hotkeys.ToKeyStrokes())
			.ConfigureAwait(false);

		if (result.IsSaved)
		{
			await _fileHotkeyEditor
				.OverwriteAsync(dto, result.NewHotkeys, Hierarchy)
				.ConfigureAwait(false);
		}

		_backgroundServiceControl.ResumeHotkeyTracking();
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

	/// <inheritdoc cref="IBackgroundServiceControl" />
	private readonly IBackgroundServiceControl _backgroundServiceControl;

	/// <inheritdoc cref="IClipboardLogSeeder" />
	private readonly IClipboardLogSeeder _clipboardLogSeeder;

	/// <inheritdoc cref="IClipboardTextWriter" />
	private readonly IClipboardTextWriter _clipboardWriter;

	/// <inheritdoc cref="IContentCopier" />
	private readonly IContentCopier _contentCopier;

	/// <inheritdoc cref="IDataExchangeService" />
	private readonly IDataExchangeService _dataExchange;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IFileContentsReader" />
	private readonly IFileContentsReader _fileContentsReader;

	/// <inheritdoc cref="IFileHotkeyEditor" />
	private readonly IFileHotkeyEditor _fileHotkeyEditor;

	/// <inheritdoc cref="IHierarchyEditor" />
	private readonly IHierarchyEditor _hierarchyEditor;

	/// <summary>
	/// Objects to highlight where the view shows them.
	/// </summary>
	private readonly Subject<ExplorerItemDtoBase> _highlightSignal = new();

	/// <inheritdoc cref="IMainWindowLauncher" />
	private readonly IMainWindowLauncher _mainWindowLauncher;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <inheritdoc cref="INoteEditor" />
	private readonly INoteEditor _noteEditor;

	/// <inheritdoc cref="INoteReader" />
	private readonly INoteReader _noteReader;

	/// <inheritdoc cref="IOpenFilesCloser" />
	private readonly IOpenFilesCloser _openFilesCloser;

	/// <inheritdoc cref="IProcessManager" />
	private readonly IProcessManager _processManager;

	/// <inheritdoc cref="IEntityPropertyWriter" />
	private readonly IEntityPropertyWriter _propertyWriter;

	/// <inheritdoc cref="IProtectionScenarios" />
	private readonly IProtectionScenarios _protectionScenarios;

	/// <inheritdoc cref="ISampleSeeder" />
	private readonly ISampleSeeder _sampleSeeder;

	/// <inheritdoc cref="ISessionReadOnlyMode" />
	private readonly ISessionReadOnlyMode _sessionReadOnlyMode;

	/// <inheritdoc cref="ShowSignal" />
	private readonly Subject<ExplorerItemDtoBase> _showSignal = new();

	/// <inheritdoc cref="ISettingsCommit" />
	private readonly ISettingsCommit _settingsCommit;

	/// <summary>
	/// Number of operations showing the progress bar.
	/// </summary>
	private int _actionsInProgress;
	#endregion

	#region Constructors
	public EditorViewModel(
		Application app,
		EditingFilesViewModel editingFiles,
		IAppSettingsStore settingsStore,
		IAutoLockService autoLock,
		IBackgroundServiceControl backgroundServiceControl,
		IClipboardLogSeeder clipboardLogSeeder,
		IClipboardTextWriter clipboardWriter,
		IContentCopier contentCopier,
		IContentVisibility contentVisibility,
		ICopyHistory copyHistory,
		IDataExchangeService dataExchange,
		IDbFailureReporter dbFailureReporter,
		IDialogService dialogService,
		IDispatcherAccessor dispatcher,
		IEntityPropertyWriter propertyWriter,
		IExecutingFiles executingFiles,
		IFileContentsReader fileContentsReader,
		IFileHotkeyEditor fileHotkeyEditor,
		IHierarchyEditor hierarchyEditor,
		ILogger logger,
		IMainWindowLauncher mainWindowLauncher,
		IMessenger messenger,
		INoteEditor noteEditor,
		INoteReader noteReader,
		INotificationService notification,
		IOpenFilesCloser openFilesCloser,
		IProcessManager processManager,
		IProtectionScenarios protectionScenarios,
		ISampleSeeder sampleSeeder,
		ISecondaryWindowLauncher secondaryWindowLauncher,
		ISessionEditorTabs sessionEditorTabs,
		ISessionHierarchy sessionHierarchy,
		ISessionReadOnlyMode sessionReadOnlyMode,
		ISettingsCommit settingsCommit,
		ITaskExceptionHandler exceptionHandler,
		IViewFactory viewFactory) : base(
			app,
			settingsStore,
			contentVisibility,
			copyHistory,
			dialogService,
			dispatcher,
			executingFiles,
			logger,
			messenger,
			notification,
			secondaryWindowLauncher,
			sessionEditorTabs,
			sessionHierarchy,
			exceptionHandler,
			viewFactory)
	{
		EditingFiles = editingFiles;

		_autoLock = autoLock;

		_backgroundServiceControl = backgroundServiceControl;

		_clipboardLogSeeder = clipboardLogSeeder;

		_clipboardWriter = clipboardWriter;

		_contentCopier = contentCopier;

		_dataExchange = dataExchange;

		_dbFailureReporter = dbFailureReporter;

		_fileContentsReader = fileContentsReader;

		_fileHotkeyEditor = fileHotkeyEditor;

		_hierarchyEditor = hierarchyEditor;

		_mainWindowLauncher = mainWindowLauncher;

		_noteEditor = noteEditor;

		_noteReader = noteReader;

		_openFilesCloser = openFilesCloser;

		_processManager = processManager;

		_protectionScenarios = protectionScenarios;

		_propertyWriter = propertyWriter;

		_sampleSeeder = sampleSeeder;

		_sessionReadOnlyMode = sessionReadOnlyMode;

		_settingsCommit = settingsCommit;

		// The expanded folders and the selected object of the tree are kept for the next start.
		Hierarchy
			.MergeManyInTree(x => x.WhenPropertyChanged(y => y.IsExpanded, notifyOnInitialValue: false))
			.Subscribe(x => SaveIsExpanded(x.Sender.Id, x.Value))
			.DisposeWith(_disposables);

		Hierarchy
			.MergeManyInTree(x => x.WhenPropertyChanged(y => y.IsSelected, notifyOnInitialValue: false))
			.Subscribe(x => SaveIsSelected(x.Sender))
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
			NavigationColumnWidth = new GridLength(windowSettings.NavigationColumnWidth);
		}

		IsReadOnly = windowSettings.IsReadOnly;

		IsInitialized = true;
	}

	/// <inheritdoc />
	public void Receive(CloseEditingFileMessage message) => CloseEditingFile(message.File);

	/// <inheritdoc />
	public void Receive(ConfirmUpdateMessage message)
	{
		message.Reply(_dialogService.RequestYesNoAsync(message.Text, token: message.Token));
	}

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
	/// Sets the <see cref="ExplorerItemDtoBase.IsSelected" /> to <c>True</c> and <see cref="SelectedObject" /> from <paramref name="selected"/>.
	/// </summary>
	public void SetSelectedObject(ExplorerItemDtoBase selected)
	{
		selected.IsSelected = true;

		SelectedObject = selected;
	}

	/// <inheritdoc />
	/// <remarks>
	/// The editor shows the object in its own tree, whichever window asks.
	/// </remarks>
	public override Task ShowInEditorAsync(
		Guid id,
		Window window,
		CancellationToken token = default)
	{
		ShowObject(id);

		return Task.CompletedTask;
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
		if (!await _hierarchyEditor
			.DeleteAsync(dto, Hierarchy, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		// A folder leaves the database together with every file of its subtree.
		FileDto[] deletedFiles = dto switch
		{
			FileDto file => [file],
			FolderDto folder => [.. folder.Children.GetFiles()],
			_ => []
		};

		foreach (FileDto deletedFile in deletedFiles)
		{
			CloseFile(deletedFile);

			_copyHistory.Remove(deletedFile);
		}

		if (dto is FolderDto deletedFolder)
		{
			_contentVisibility.DiscardKeys(deletedFolder);
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

		FolderDto[] folders = [.. Hierarchy.GetFoldersBy(x => x.IsExpanded != isExpanded)];

		if (folders.IsEmpty())
		{
			return Task.CompletedTask;
		}

		return folders.ForEachAsync(x => x.IsExpanded = isExpanded, Environment.ProcessorCount);
	}

	/// <summary>
	/// Sets the default <see cref="NavigationColumnWidth" /> for a window <paramref name="windowWidth" /> wide.
	/// </summary>
	internal void SetDefaultNavigationColumnWidth(double windowWidth)
	{
		NavigationColumnWidth = new GridLength(windowWidth / 3.0);
	}

	/// <summary>
	/// Refreshes the command that hides everything and keeps the auto-lock countdown
	/// in step with the decrypted contents.
	/// </summary>
	protected internal override void NotifyDecryptedContentsChanged()
	{
		HideAllFileContentsCommand.NotifyCanExecuteChanged();

		_backgroundServiceControl.RefreshAutoLock();
	}

	/// <inheritdoc />
	protected override void CloseEditingFile(FileDto file) => EditingFiles.CloseTab(file);
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
			&& (dto is not null || SelectedObject is not null);
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
	private bool CanResetSelectedObject() => SelectedObject is not null;

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
	/// Asks the view to highlight an object where it shows it.
	/// </summary>
	private void Highlight(ExplorerItemDtoBase dto) => _highlightSignal.OnNext(dto);

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
	private void SaveIsSelected(ExplorerItemDtoBase item)
	{
		if (IsReadOnly
			|| IsActionInProgress
			|| _app.HasWindow<EditorWindow>(x => !x.IsLoaded || !x.IsVisible))
		{
			return;
		}

		_exceptionHandler.Watch(_propertyWriter.UpdateIsSelectedAsync(item));
	}

	/// <summary>
	/// Adds to the tree the run of samples that <paramref name="seed" /> writes to the database, with the menu closed and the
	/// progress bar on.
	/// </summary>
	private async Task SeedAsync(string logMessage, Func<CancellationToken, Task<FolderDto>> seed)
	{
		IsLeftDrawerOpened = false;

		_logger.LogInformation(logMessage);

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			FolderDto run = await seed(CancellationToken.None).ConfigureAwait(true);

			AddHierarchy([run]);
		}
		catch (Exception ex)
		{
			_logger.LogException("The sample objects could not be added.", ex);
		}
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
	/// Expands the folders of an object and asks the tree to bring the object into view, select and highlight it.
	/// </summary>
	private void ShowObject(Guid id)
	{
		if (Hierarchy.FindById(id) is not { } found)
		{
			return;
		}

		foreach (FolderDto parent in found.GetAllParents())
		{
			parent.IsExpanded = true;
		}

		_showSignal.OnNext(found);
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
