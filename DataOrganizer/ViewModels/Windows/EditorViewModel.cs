using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Enums.Views;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Execution;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Updates;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Windows;
using Entities.Enums;
using Entities.Models;
using Material.Styles.Controls;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BrushExtensions = DataOrganizer.Extensions.BrushExtensions;

namespace DataOrganizer.ViewModels.Windows;

/// <summary>
/// View model for <c>EditorWindow</c>.
/// </summary>
public partial class EditorViewModel :
	ViewModelBase,
	INavigationColumnViewModel,
	IRecipient<FolderExpandedChangedMessage>,
	IRecipient<ShowInEditorMessage>,
	IRecipient<ShowProgressBarMessage>,
	IUpdatePrompt
{
	#region Properties
	/// <inheritdoc cref="IAutoLockService" />
	public IAutoLockService AutoLock => _autoLock;

	/// <summary>
	/// Information in the lower left corner.
	/// </summary>
	[ObservableProperty]
	public partial string? HierarchySummary { get; set; }

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

	/// <summary>
	/// <c>True</c> when the contents cannot be edited.
	/// </summary>
	[ObservableProperty]
	public partial bool IsReadOnly { get; set; }

	/// <summary>
	/// <c>True</c> when the right side sheet should be opened.
	/// </summary>
	[ObservableProperty]
	public partial bool IsRightSideSheetOpened { get; set; }

	/// <inheritdoc cref="INavigationColumnViewModel.NavigationColumnWidth" />
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
	/// The current width of the window.
	/// </summary>
	[ObservableProperty]
	public partial double ViewWidth { get; set; }
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsLeftDrawerOpened" /> changes.
	/// </summary>
	partial void OnIsLeftDrawerOpenedChanged(bool value) => ShowFavoritesCommand.NotifyCanExecuteChanged();

	/// <summary>
	/// Called when <see cref="IsReadOnly" /> changes.
	/// </summary>
	partial void OnIsReadOnlyChanged(bool value)
	{
		AddCommand.NotifyCanExecuteChanged();

		DeleteCommand.NotifyCanExecuteChanged();

		_messenger.Send(new EditorReadOnlyModeChangedMessage(value));

		_logger.LogDebug(
			$@"""{nameof(IsReadOnly)}"" property of ""{nameof(EditorViewModel)}"" has changed to ""{value}""");
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

		if (RightSideSheetContent == RightSideSheetContentKind.CopyHistory)
		{
			SaveCopyHistory();
		}

		RightSideSheetContent = RightSideSheetContentKind.None;
	}

	/// <summary>
	/// Called when <see cref="SelectedObject" /> changes.
	/// </summary>
	partial void OnSelectedObjectChanging(
		ExplorerItemDtoBase? oldValue,
		ExplorerItemDtoBase? newValue)
	{
		if (IsReadOnly
			|| IsActionInProgress
			|| _app.HasWindow<EditorWindow>(x => !x.IsLoaded || !x.IsVisible))
		{
			return;
		}

		// When an object is removed from a collection, its selection is reset and its existence must be checked
		// to ensure that no attempt is made to save properties to the database for a non-existent object.
		if (oldValue is not null && Hierarchy.ContainsId(oldValue.Id))
		{
			_exceptionHandler.Watch(_propertyWriter.UpdateIsSelectedAsync(oldValue));
		}

		if (newValue is null)
		{
			return;
		}

		_exceptionHandler.Watch(_propertyWriter.UpdateIsSelectedAsync(newValue));
	}

	/// <summary>
	/// Called when <see cref="ViewWidth" /> changes.
	/// </summary>
	partial void OnViewWidthChanged(double value) => ((INavigationColumnViewModel)this).SetNavigationColumnWidth(value);
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Changes password for folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanChangePassword))]
	internal async Task ChangePassword(FolderDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileDto[] openedFiles = [.. dto
			.Children
			.GetFilesBy(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Change password of the folder");

		await _folderProtection
			.ChangePasswordAsync(dto)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Decrypts files in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanDecryptFolder))]
	internal async Task DecryptFolder(FolderDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.IsEmpty())
		{
			_notification.ShowInformationSnackbar(Strings.MissingFiles);

			return;
		}

		FileDto[] openedFiles = [.. files.Where(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Decrypt files in a folder");

		await _folderProtection
			.DecryptFolderAsync(dto, files)
			.ConfigureAwait(false);
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
			.EditAsync(dto, result.Value, DateTime.Now)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Encrypts files in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanEncryptFolder))]
	internal async Task EncryptFolder(FolderDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.IsEmpty())
		{
			_notification.ShowInformationSnackbar(Strings.MissingFiles);

			return;
		}

		FileDto[] openedFiles = [.. files.Where(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Encrypt files in a folder");

		await _folderProtection
			.EncryptFolderAsync(dto, files)
			.ConfigureAwait(false);
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

		if (_executionEngine.IsExecuting(dto.Id))
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

		ValidatedContents result = await _dbAccess
			.GetFileContentsAsync(dto.Id)
			.ConfigureAwait(true);

		if (!result.IsValid)
		{
			string errorText = $@"{Strings.FailedToLoadFileContents} ""{dto.Name}""";

			_notification.ShowErrorSnackbar(errorText);

			_logger.LogError($"{errorText}:{dto.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Kind))}");

			return;
		}

		_logger.LogInformation(
			$"Contents of the {result.Contents.Length}-byte file loaded from the database:{dto.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Name),
				nameof(FileDto.Kind),
				nameof(FileDto.UpdatedAt))}");

		byte[] contents = result.Contents;

		Guid? keeperId = null;

		if (dto.EncryptionStatus == EncryptionStatus.Decrypted && dto.FindPasswordKeeper() is { } keeper)
		{
			keeperId = keeper.Id;

			try
			{
				contents = _contentCipher.Decrypt(dto, contents);
			}
			catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
			{
				_logger.LogException(ex);

				_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

				return;
			}
		}

		ExecuteFileParameters parameters = new()
		{
			Contents = contents,
			File = dto,
			IsReadOnly = IsReadOnly,
			KeeperId = keeperId,
		};

		if (!await _executionEngine
			.ExecuteAsync(parameters)
			.ConfigureAwait(true))
		{
			return;
		}

		dto.IsExecuting = true;

		ExecutingFiles.Add(dto);
	}

	/// <summary>
	/// Exits the application.
	/// </summary>
	[RelayCommand]
	internal void Exit(Window? window)
	{
		IsShutdown = true;

		window?.Close();
	}

	/// <summary>
	/// Hides all file contents.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanHideAllFileContents))]
	internal async Task HideAllFileContents()
	{
		// The contents cannot be hidden while an editor holds changes it was unable to persist.		
		if (!await TryFlushEditorsAsync().ConfigureAwait(true))
		{
			return;
		}

		FileDto[] openedFiles = [.. Hierarchy.GetFilesBy(x => x.IsOpened() && x.EncryptionStatus == EncryptionStatus.Decrypted)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_contentVisibility.HideAllContents(Hierarchy);

		NotifyDecryptedContentsChanged();
	}

	/// <summary>
	/// Hides file contents.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanHideFileContents))]
	internal async Task HideFileContents(FileDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		// The contents cannot be hidden while an editor holds changes it was unable to persist.
		if (!await TryFlushEditorsAsync().ConfigureAwait(true))
		{
			return;
		}

		if (dto.IsOpened())
		{
			if (!await _dialogService
				.RequestCloseFilesAsync()
				.ConfigureAwait(true))
			{
				return;
			}

			CloseFile(dto);
		}

		_logger.LogInformation("Hide file contents");

		_contentVisibility.HideFileContents(dto);

		NotifyDecryptedContentsChanged();
	}

	/// <inheritdoc cref="IContentVisibility.HideFolderContents" />
	[RelayCommand(CanExecute = nameof(CanHideFolderContents))]
	internal async Task HideFolderContents(FolderDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		// The contents cannot be hidden while an editor holds changes it was unable to persist.
		if (!await TryFlushEditorsAsync().ConfigureAwait(true))
		{
			return;
		}

		FileDto[] openedFiles = [.. dto
			.Children
			.GetFilesBy(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Hide files in a folder");

		_contentVisibility.HideFolderContents(dto);

		NotifyDecryptedContentsChanged();
	}

	/// <summary>
	/// Imports data.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanImport))]
	internal async Task Import()
	{
		FileDto[] openedFiles = [.. Hierarchy.GetFilesBy(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
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
			CopyHistorySettings
				.ItemIds
				.Clear();

			IsRightSideSheetOpened = false;

			_contentVisibility.DiscardAllKeys();
		}

		AddHierarchy(result.ImportedItems);

		_notification.ShowInformationSnackbar(Strings.DataImportCompleted);
	}

	/// <summary>
	/// Resets the <see cref="SelectedObject" />.
	/// </summary>
	/// <remarks>
	/// Change to the <see cref="ExplorerItemDtoBase.IsSelected" /> property is saved to the database
	/// using the <see cref="OnSelectedObjectChanging(ExplorerItemDtoBase?, ExplorerItemDtoBase?)" /> method.
	/// </remarks>
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
	internal Task SetFavorite(FileDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		dto.IsFavorite = !dto.IsFavorite;

		return _propertyWriter.UpdateIsFavoriteAsync(dto);
	}

	/// <summary>
	/// Displays the "Favorites" window.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowFavorites))]
	internal void ShowFavorites(EditorWindow? window)
	{
		IsShutdown = false;

		window?.Close();

		_copyHistory?.Dispose();

		_viewLauncher.CreateFavoritesWindow(
			Hierarchy,
			_editingFiles?.Items ?? [],
			ExecutingFiles).Show();

		if (_editingFiles is null)
		{
			return;
		}

		Hierarchy
			.GetFilesBy(x => x.IsEditing)
			.ForEach(_editingFiles.CloseEditor);
	}

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowFolderContents))]
	internal async Task ShowFolderContents(FolderDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.IsEmpty())
		{
			_notification.ShowInformationSnackbar(Strings.MissingFiles);

			return;
		}

		FileDto[] openedFiles = [.. files.Where(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Show file contents in a folder");

		await _contentVisibility
			.ShowFolderContentsAsync(dto)
			.ConfigureAwait(true);

		NotifyDecryptedContentsChanged();
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
			if (CopyHistorySettings.ItemIds.Count == 0 || !await _dialogService
				.RequestYesCancelAsync($"{Strings.Clear}?")
				.ConfigureAwait(false))
			{
				return;
			}

			ClearCopyHistory();
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

	/// <inheritdoc cref="CloseFile" />
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

	/// <inheritdoc cref="CopyContentViewModelBase.CopyContentAsync" />
	[RelayCommand(CanExecute = nameof(CanCopyContent))]
	private Task CopyContentByContextMenu(FileDto? dto)
	{
		if (dto is null
			|| _app.FindWindow<EditorWindow>() is not { } window
			|| window.FindLogicalDescendantOfType<TreeView>() is not { } container)
		{
			return Task.CompletedTask;
		}

		return CopyContentAsync(
			file: dto,
			container: container,
			updateView: true);
	}

	/// <summary>
	/// Copies object's name to clipboard.
	/// </summary>
	[RelayCommand]
	private void CopyName(ExplorerItemDtoBase? dto)
	{
		try
		{
			if (dto is null
				|| _app.FindWindow<EditorWindow>() is not { } window
				|| window.FindLogicalDescendantOfType<TreeView>() is not { } container)
			{
				return;
			}

			_exceptionHandler.Watch(_clipboard.SetTextAsync(dto.Name));

			FolderDto[] parents = [.. dto
				.GetAllParents()
				.Reverse()];

			if (FindLastContainer(container, parents)?.ContainerFromItem(dto) is not TemplatedControl item)
			{
				return;
			}

			_exceptionHandler.Watch(BrushExtensions.ApplyHighlightAnimationAsync(() => item.Background as Brush));
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

		bool isopened = dto is FileDto file && file.IsOpened();

		if (!await _dialogService
			.RequestYesNoAsync($@"{(isopened ? Strings.CloseTheFileAndDelete : Strings.Delete)} ""{toBeDeleted.Name}""?")
			.ConfigureAwait(true))
		{
			return;
		}

		// The file is closed next.
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

		_editingFiles?.OpenInEditor(dto);
	}

	/// <summary>
	/// Handles loading event for rendering the file editor.
	/// </summary>
	[RelayCommand]
	private void EditingFilesViewLoaded(EditingFilesViewModel? viewModel)
	{
		viewModel?
			.Items
			.AddRange(OpenedInEditorFiles);

		_editingFiles = viewModel;
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
	/// Opens a file context menu.
	/// </summary>
	[RelayCommand]
	private void OpenFileContextMenu(Control? container)
	{
		if (container is null)
		{
			return;
		}

		if (Interaction
			.GetBehaviors(container)
			.OfType<LazyContextFlyoutBehavior>()
			.FirstOrDefault() is { } behavior)
		{
			behavior.Show();
		}
		else if (container.ContextFlyout is { } flyout)
		{
			flyout.ShowAt(container);
		}
		else
		{
			return;
		}

		if (SelectedObject is null)
		{
			return;
		}

		_logger.LogDebug($"Open a file context menu:{SelectedObject.GetPropertyValues(
			true,
			nameof(FileDto.Id),
			nameof(FileDto.Name))}");
	}

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
			.RenameAsync(toBeRenamed, pair.Key, DateTime.Now)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Starts the auto-lock countdown over from the delay currently in the settings.
	/// </summary>
	[RelayCommand]
	private void RestartAutoLock() => _autoLock.Arm();

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
		if (RightSideSheetContent == RightSideSheetContentKind.CopyHistory)
		{
			SaveCopyHistory();
		}

		SwitchRightSideSheetContent(RightSideSheetContentKind.ExecutingFiles);
	}

	/// <inheritdoc cref="IContentVisibility.ShowFileContentsAsync" />
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

		if (_keyboardInputHook.IsValueCreated && _keyboardInputHook.Value.IsRunning)
		{
			await _keyboardInputHook
				.Value
				.StopTrackingAsync()
				.ConfigureAwait(true);
		}

		EditingHotkeysResult result = await _dialogService
			.EditHotkeysAsync(dto.Hotkeys.ToKeyStrokes())
			.ConfigureAwait(false);

		if (result.IsSaved)
		{
			await _fileHotkeyEditor
				.OverwriteAsync(dto, result.NewHotkeys, Hierarchy)
				.ConfigureAwait(false);
		}

		if (!_settingsStore
			.Settings
			.TrackHotkeys)
		{
			return;
		}

		_exceptionHandler.Watch(_keyboardInputHook.Value.StartTrackingAsync(Hierarchy));
	}

	/// <inheritdoc cref="ViewModelBase.ShowInEditorAsync" />
	[RelayCommand]
	private void ShowInList(Guid id)
	{
		if (_app.FindWindow<EditorWindow>() is not { } window)
		{
			return;
		}

		_exceptionHandler.Watch(ShowInEditorAsync(id, window));
	}

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

		ShowSettingsResult result = await _dialogService
			.ShowSettingsAsync()
			.ConfigureAwait(true);

		await HandleSettingsChangedAsync(
			result.IsSaved,
			result.Settings).ConfigureAwait(false);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IAutoLockService" />
	private readonly IAutoLockService _autoLock;

	/// <inheritdoc cref="IClipboardLogService" />
	private readonly IClipboardLogService _clipboardLog;

	/// <inheritdoc cref="IClipboardLogPersistenceCoordinator" />
	private readonly IClipboardLogPersistenceCoordinator _clipboardLogPersistence;

	/// <inheritdoc cref="IDataExchangeService" />
	private readonly IDataExchangeService _dataExchange;

	/// <inheritdoc cref="IFileHotkeyEditor" />
	private readonly IFileHotkeyEditor _fileHotkeyEditor;

	/// <inheritdoc cref="IFolderProtection" />
	private readonly IFolderProtection _folderProtection;

	/// <inheritdoc cref="IHierarchyEditor" />
	private readonly IHierarchyEditor _hierarchyEditor;

	/// <inheritdoc cref="INoteEditor" />
	private readonly INoteEditor _noteEditor;

	/// <inheritdoc cref="INoteReader" />
	private readonly INoteReader _noteReader;

	/// <inheritdoc cref="IProcessManager" />
	private readonly IProcessManager _processManager;

	/// <inheritdoc cref="IEntityPropertyWriter" />
	private readonly IEntityPropertyWriter _propertyWriter;

	/// <inheritdoc cref="IAppThemeService" />
	private readonly IAppThemeService _themeService;

	/// <inheritdoc cref="EditingFilesViewModel" />
	private EditingFilesViewModel? _editingFiles;
	#endregion

	#region Constructors
	public EditorViewModel(
		Application app,
		IAppSettingsStore settingsStore,
		IAppThemeService themeService,
		IAutoLockService autoLock,
		IClipboardAccessor clipboard,
		IClipboardLogService clipboardLog,
		IClipboardLogPersistenceCoordinator clipboardLogPersistence,
		IContentCipher contentCipher,
		IContentVisibility contentVisibility,
		IDataExchangeService dataExchange,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IDispatcherAccessor dispatcher,
		IEntityPropertyWriter propertyWriter,
		IExecutionEngine executionEngine,
		IFileHotkeyEditor fileHotkeyEditor,
		IFolderProtection folderProtection,
		IHierarchyEditor hierarchyEditor,
		ILogger logger,
		IMessenger messenger,
		INoteEditor noteEditor,
		INoteReader noteReader,
		INotificationService notification,
		IProcessManager processManager,
		ITaskExceptionHandler exceptionHandler,
		IViewLauncher viewLauncher,
		Lazy<IKeyboardInputHook> keyboardInputHook) : base(
			app,
			settingsStore,
			clipboard,
			contentCipher,
			contentVisibility,
			dbAccess,
			dialogService,
			dispatcher,
			executionEngine,
			logger,
			messenger,
			notification,
			exceptionHandler,
			viewLauncher,
			keyboardInputHook)
	{
		_autoLock = autoLock;

		_folderProtection = folderProtection;

		_clipboardLog = clipboardLog;

		_clipboardLogPersistence = clipboardLogPersistence;

		_dataExchange = dataExchange;

		_fileHotkeyEditor = fileHotkeyEditor;

		_hierarchyEditor = hierarchyEditor;

		_noteEditor = noteEditor;

		_noteReader = noteReader;

		_processManager = processManager;

		_propertyWriter = propertyWriter;

		_themeService = themeService;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void AddHierarchy(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		Hierarchy.AddRange(hierarchy);

		UpdateHierarchySummary();
	}

	/// <inheritdoc />
	public Task<bool> ConfirmUpdateAsync(
		string text,
		CancellationToken token = default)
	{
		return _dialogService.RequestYesNoAsync(text, token);
	}

	/// <summary>
	/// Restores the window placement and the copy history from the stored settings.
	/// </summary>
	public void Initialize(
		Window window,
		EditorWindowSettings windowSettings,
		CopyHistoryViewSettings copyHistorySettings)
	{
		if (windowSettings.Size is { Width: > 0, Height: > 0 })
		{
			window.Width = windowSettings.Size.Width;

			window.Height = windowSettings.Size.Height;
		}
		else
		{
			IViewLauncher.SetDefaultSize(window);
		}

		PixelPoint savedPosition = new(windowSettings.X, windowSettings.Y);

		if (windowSettings.X > 0
			&& windowSettings.Y > 0
			&& IViewLauncher.IsWindowPositionOnScreen(window, savedPosition))
		{
			window.Position = savedPosition;
		}
		else
		{
			IViewLauncher.SetDefaultLocation(window);
		}

		if (windowSettings.WindowState != WindowState.Minimized)
		{
			window.WindowState = windowSettings.WindowState;
		}

		if (windowSettings.NavigationColumnWidth > default(double))
		{
			NavigationColumnWidth = new GridLength(windowSettings.NavigationColumnWidth);
		}
		else
		{
			IViewLauncher.SetDefaultNavigationColumnWidth(window, this);
		}

		IsReadOnly = windowSettings.IsReadOnly;

		CopyHistorySettings.AddItemIds(copyHistorySettings.ItemIds, Hierarchy);

		if (CopyHistorySettings.ItemIds.Count > 0)
		{
			CopyHistorySettings.SelectedItemId = copyHistorySettings.SelectedItemId;
		}

		IsInitialized = true;
	}

	/// <summary>
	/// <inheritdoc />
	/// </summary>
	/// <remarks>
	/// There was no way to track the expand/collapse events of <see cref="TreeViewItem" /> in Xaml,
	/// so I had to use a global message to persist the changes to the database in one place.
	/// </remarks>
	public void Receive(FolderExpandedChangedMessage message)
	{
		if (IsReadOnly || IsActionInProgress)
		{
			return;
		}

		_exceptionHandler.Watch(_propertyWriter.UpdateIsExpandedAsync(message.Id, message.IsExpanded));
	}

	/// <inheritdoc />
	public void Receive(ShowInEditorMessage message)
	{
		_exceptionHandler.Watch(ShowInEditorAsync(message.Id, message.Window));
	}

	/// <inheritdoc />
	public void Receive(ShowProgressBarMessage message)
	{
		IsActionInProgress = message.IsVisible;
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
	public override async Task ShowInEditorAsync(
		Guid id,
		Window window,
		CancellationToken token = default)
	{
		if (Hierarchy.FindById(id) is not { } found)
		{
			return;
		}

		FolderDto[] parents = [.. found
			.GetAllParents()
			.ForEach(x => x.IsExpanded = true)
			.Reverse()];

		TreeView? treeView = null;

		Func<bool> condition = () =>
		{
			treeView = window.FindDescendantOfType<TreeView>();

			return treeView is not null;
		};

		const int delay = 200;

		if (!await condition
			.WaitAsync(delay, 10, token)
			.ConfigureAwait(true) || treeView is null)
		{
			return;
		}

		treeView.ScrollIntoView(found);

		if (treeView.FindDescendantOfType<ScrollViewer>() is { } scrollViewer)
		{
			scrollViewer.Offset = new(
				int.MaxValue,
				scrollViewer.Offset.Y);
		}

		SetSelectedObject(found);

		if (FindLastContainer(treeView, parents)?.ContainerFromItem(found) is not TemplatedControl item)
		{
			return;
		}

		await Task
			.Delay(delay, token)
			.ConfigureAwait(true);

		await BrushExtensions.ApplyHighlightAnimationAsync(
			() => item.Background as Brush,
			token).ConfigureAwait(false);
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

		if (dto is FileDto file)
		{
			CloseFile(file);

			RemoveFromCopyHistory(file);
		}
		else if (dto is FolderDto folder)
		{
			_contentVisibility.DiscardKeys(folder);
		}

		UpdateHierarchySummary();

		return true;
	}

	/// <summary>
	/// Expands or collapses all folders in <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	/// <remarks>
	/// Changes to the <see cref="ExplorerItemDtoBase.IsExpanded" /> property of folders are saved to the database
	/// using the <see cref="Receive(FolderExpandedChangedMessage)" /> message handler.
	/// </remarks>
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
	/// Handles changing application settings.
	/// </summary>
	internal async Task HandleSettingsChangedAsync(
		bool isSave,
		AppSettings settings,
		CancellationToken token = default)
	{
		// Captured before overwrite so we can detect a persistence ON -> OFF transition below.
		bool wasPersistingClipboard = _settingsStore
			.Settings
			.PersistClipboardHistory;

		// Captured before overwrite so that only a changed delay restarts the countdown.
		int previousAutoLockMinutes = _settingsStore
			.Settings
			.AutoLockMinutes;

		if (isSave)
		{
			_settingsStore.Overwrite(settings);

			_settingsStore.Save();
		}
		else
		{
			_themeService.ApplyFromSettings();
		}

		if (!isSave)
		{
			return;
		}

		// A changed delay takes effect at once: the countdown restarts from it, and no auto-lock stops it.
		if (settings.AutoLockMinutes != previousAutoLockMinutes)
		{
			NotifyDecryptedContentsChanged();
		}

		await ApplyClipboardHistorySettingAsync(
			settings.TrackClipboardHistory,
			token).ConfigureAwait(false);

		if (wasPersistingClipboard && (!settings.PersistClipboardHistory || !settings.TrackClipboardHistory))
		{
			_clipboardLogPersistence.DisablePersistence();
		}

		if (_keyboardInputHook.IsValueCreated && _keyboardInputHook.Value.IsRunning)
		{
			await _keyboardInputHook
				.Value
				.StopTrackingAsync(token)
				.ConfigureAwait(false);
		}

		if (!settings.TrackHotkeys)
		{
			return;
		}

		_exceptionHandler.Watch(_keyboardInputHook.Value.StartTrackingAsync(Hierarchy, token));
	}

	/// <summary>
	/// Refreshes the command that hides everything and keeps the auto-lock countdown
	/// in step with the decrypted contents.
	/// </summary>
	protected internal override void NotifyDecryptedContentsChanged()
	{
		HideAllFileContentsCommand.NotifyCanExecuteChanged();

		if (CanHideAllFileContents())
		{
			_autoLock.Arm();
		}
		else
		{
			_autoLock.Stop();
		}
	}

	/// <inheritdoc />
	protected override void CloseEditingFile(FileDto file)
	{
		if (_editingFiles is not null)
		{
			_editingFiles.CloseTab(file);
		}
		else
		{
			file.IsEditing = false;
		}
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
	/// Starts or stops clipboard history tracking to match <paramref name="isEnabled" />,
	/// dropping the in-memory history when disabled.
	/// </summary>
	private async Task ApplyClipboardHistorySettingAsync(bool isEnabled, CancellationToken token)
	{
		IsClipboardHistoryEnabled = isEnabled;

		if (isEnabled)
		{
			if (!_clipboardLog.IsRunning)
			{
				_exceptionHandler.Watch(_clipboardLog.StartAsync(token));
			}

			return;
		}

		if (_clipboardLog.IsRunning)
		{
			_clipboardLog.Stop();
		}

		await _clipboardLog
			.ClearEntriesAsync()
			.ConfigureAwait(false);
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
			&& dto?.IsPasswordKeeper() == true;
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
			&& dto?.IsPasswordKeeper() == true
			&& dto.EncryptionStatus == EncryptionStatus.Encrypted
			&& dto.Children.AllBy(x => x.EncryptionStatus == EncryptionStatus.Encrypted);
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
		// A folder can be encrypted only while it belongs to no keeper, itself included.
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto is not null
			&& dto.FindPasswordKeeper() is null
			&& !dto.AnyChild(x => x.EncryptionStatus != EncryptionStatus.None);
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
	/// Clears copy history.
	/// </summary>
	private void ClearCopyHistory()
	{
		_copyHistory?.Clear();

		SaveCopyHistory();
	}

	/// <summary>
	/// Tries to remove value from copy history.
	/// </summary>
	private void RemoveFromCopyHistory(FileDto file)
	{
		CopyHistorySettings
			.ItemIds
			.Remove(file.Id);

		_copyHistory?.Remove(file);
	}

	/// <inheritdoc cref="IContentVisibility.ShowFileContentsAsync" />
	private async Task<bool> ShowFileContentsAsync(FileDto dto)
	{
		_logger.LogInformation("Show file contents");

		if (!await _contentVisibility
			.ShowFileContentsAsync(dto)
			.ConfigureAwait(true))
		{
			return false;
		}

		NotifyDecryptedContentsChanged();

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
	/// Tries to close editing or executing files if any.
	/// </summary>
	private async Task<bool> TryCloseOpenedFilesAsync(
		FileDto[] openedFiles,
		CancellationToken token = default)
	{
		if (openedFiles.IsNotEmpty())
		{
			if (!await _dialogService
				.RequestCloseFilesAsync(token)
				.ConfigureAwait(true))
			{
				return false;
			}

			CloseFiles(
				openedFiles.Where(x => x.IsEditing),
				openedFiles.Where(x => x.IsExecuting));
		}

		return true;
	}

	/// <summary>
	/// Persists the pending changes of the open editors before the key is dropped;
	/// <c>False</c> reports the failure to the user and keeps the caller from hiding anything.
	/// </summary>
	private async Task<bool> TryFlushEditorsAsync(CancellationToken token = default)
	{
		if (await FlushEditorsAsync(token).ConfigureAwait(true))
		{
			return true;
		}

		_logger.LogWarning("Contents are not hidden: an editor failed to persist its changes");

		_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

		return false;
	}

	/// <summary>
	/// Counts the number of objects in <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	private void UpdateHierarchySummary() => HierarchySummary = Hierarchy.GetCount().AsString();
	#endregion
}
