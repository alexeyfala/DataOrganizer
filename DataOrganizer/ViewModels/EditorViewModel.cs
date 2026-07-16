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
using Comparation;
using DataOrganizer.Behaviors;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities;
using DataOrganizer.DTO.Execution;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Messages;
using DataOrganizer.Windows;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using Material.Styles.Controls;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrushExtensions = DataOrganizer.Extensions.BrushExtensions;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>EditorWindow</c>.
/// </summary>
public partial class EditorViewModel :
	ViewModelBase,
	INavigationColumnViewModel,
	IRecipient<FolderExpandedChangedMessage>,
	IRecipient<ShowInEditorMessage>,
	IRecipient<ShowProgressBarMessage>
{
	#region Properties
	/// <summary>
	/// Information in the lower left corner.
	/// </summary>
	[ObservableProperty]
	public partial string? BottomLeftCornerInfo { get; set; }

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
	/// Read-only mode.
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

	/// <inheritdoc cref="RightSideSheetContentType" />
	[ObservableProperty]
	public partial RightSideSheetContentType RightSideSheetContent { get; set; }

	/// <summary>
	/// The selected object in <see cref="TreeView" /> from <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
	[NotifyCanExecuteChangedFor(nameof(ResetSelectedObjectCommand))]
	public partial ExplorerModelBaseDto? SelectedObject { get; set; }

	/// <summary>
	/// Window width.
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

		if (RightSideSheetContent == RightSideSheetContentType.CopyHistory)
		{
			SaveCopyHistory();
		}

		RightSideSheetContent = RightSideSheetContentType.None;
	}

	/// <summary>
	/// Called when <see cref="SelectedObject" /> changes.
	/// </summary>
	partial void OnSelectedObjectChanging(
		ExplorerModelBaseDto? oldValue,
		ExplorerModelBaseDto? newValue)
	{
		if (IsReadOnly
			|| IsActionInProgress
			|| _app.IsAnyWindow<EditorWindow>(x => !x.IsLoaded || !x.IsVisible))
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
	internal async Task ChangePassword(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] openedFiles = [.. dto
			.Children
			.GetFilesBy(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Change password of the folder");

		await _entityEncryption
			.ChangePasswordAsync(dto)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Decrypts files in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanDecryptFolder))]
	internal async Task DecryptFolder(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.IsEmpty())
		{
			ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		FileModelDto[] openedFiles = [.. files.Where(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Decrypt files in a folder");

		await _entityEncryption
			.DecryptFolderAsync(dto, files)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Encrypts files in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanEncryptFolder))]
	internal async Task EncryptFolder(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.IsEmpty())
		{
			ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		FileModelDto[] openedFiles = [.. files.Where(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Encrypt files in a folder");

		await _entityEncryption
			.EncryptFolderAsync(dto, files)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Executes the file in the operating system.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanBeEditedOrExecuted))]
	internal async Task ExecuteFile(FileModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		if (_executionEngine.IsExecuting(dto.Id))
		{
			_logger.LogWarning($"The file is already executing in the operating system:{dto.GetPropertyValues(
				true,
				nameof(FileModelDto.Id),
				nameof(FileModelDto.Name),
				nameof(FileModelDto.EntityType))}");

			return;
		}

		if (ExecutableFileHelper.IsExecutable(dto.Name))
		{
			string text =
				$"{string.Format(Strings.TheFileMayRunProgramOrScriptOnYourDevice, dto.Name, Environment.NewLine)}" +
				Environment.NewLine +
				Environment.NewLine +
				$"{Strings.OpenTheExecutableFile}?";

			if (!await _dialogService
				.RequestYesCancelDialogAsync(text)
				.ConfigureAwait(false))
			{
				return;
			}
		}

		_logger.LogInformation($"The file needs to be executed in the operating system:{dto.GetPropertyValues(
			true,
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name),
			nameof(FileModelDto.EntityType))}");

		if (dto.EncryptionStatus == EncryptionStatus.Encrypted && !await ShowFileContentsAsync(dto).ConfigureAwait(false))
		{
			return;
		}

		ContentsIsValidPair result = await _dbAccess
			.GetFileContentsAsync(dto.Id)
			.ConfigureAwait(false);

		if (!result.IsValid)
		{
			string errorText = $@"{Strings.FailedToLoadFileContents} ""{dto.Name}""";

			ShowErrorSnackbar(errorText);

			_logger.LogError($"{errorText}:{dto.GetPropertyValues(
				true,
				nameof(FileModelDto.Id),
				nameof(FileModelDto.EntityType))}");

			return;
		}

		_logger.LogInformation(
			$"Contents of the {result.Contents.Length}-byte file loaded from the database:{dto.GetPropertyValues(
				true,
				nameof(FileModelDto.Id),
				nameof(FileModelDto.Name),
				nameof(FileModelDto.EntityType),
				nameof(FileModelDto.UpdatedDate))}");

		byte[] contents = result.Contents;

		byte[]? sessionEncryptedDek = null;

		if (dto.EncryptionStatus == EncryptionStatus.Decrypted
			&& dto.FindParent(x => x.IsPasswordKeeper())?.SessionEncryptedDek is { } encryptedDek)
		{
			sessionEncryptedDek = [.. encryptedDek];

			byte[]? decryptedContents = _entityEncryption.DecryptSessionContents(
				contents,
				sessionEncryptedDek);

			if (decryptedContents is null)
			{
				ShowErrorSnackbar(Strings.FailedToProcessContents);

				return;
			}

			contents = decryptedContents;
		}

		ExecuteFileParameters parameters = new()
		{
			Contents = contents,
			File = dto,
			IsReadOnly = IsReadOnly,
			SessionEncryptedDek = sessionEncryptedDek,
		};

		if (!await _executionEngine
			.ExecuteAsync(parameters)
			.ConfigureAwait(false))
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
	[RelayCommand(CanExecute = nameof(CanHideAllFiles))]
	internal async Task HideAllFileContents()
	{
		FileModelDto[] openedFiles = [.. Hierarchy.GetFilesBy(x => x.IsOpened() && x.EncryptionStatus == EncryptionStatus.Decrypted)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		Hierarchy
			.FilterBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted)
			.ForEach(dto => dto.EncryptionStatus = EncryptionStatus.Encrypted);

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Hides file contents.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanHideFileContents))]
	internal async Task HideFileContents(FileModelDto? dto)
	{
		if (dto is null)
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

		dto.EncryptionStatus = EncryptionStatus.Encrypted;

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <inheritdoc cref="IEntityEncryption.HideFolderContents" />
	[RelayCommand(CanExecute = nameof(CanHideFolderContents))]
	internal async Task HideFolderContents(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] openedFiles = [.. dto
			.Children
			.GetFilesBy(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Hide files in a folder");

		_entityEncryption.HideFolderContents(dto, Hierarchy);

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Imports data.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanImport))]
	internal async Task Import()
	{
		FileModelDto[] openedFiles = [.. Hierarchy.GetFilesBy(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		if (await _dataExchange
			.ImportDataAsync(Hierarchy)
			.ConfigureAwait(true) is not { } result || result.Variant == ImportListVariant.None)
		{
			return;
		}

		if (result.Variant == ImportListVariant.Replace)
		{
			CopyHistorySettings
				.Items
				.Clear();

			IsRightSideSheetOpened = false;
		}

		AddHierarchy(result.ImportedItems);

		ShowInfoSnackbar(Strings.DataImportCompleted);
	}

	/// <summary>
	/// Resets the <see cref="SelectedObject" />.
	/// </summary>
	/// <remarks>
	/// Change to the <see cref="ExplorerModelBaseDto.IsSelected" /> property is saved to the database
	/// using the <see cref="OnSelectedObjectChanging(ExplorerModelBaseDto?, ExplorerModelBaseDto?)" /> method.
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

		_processUtils.StartProcess(Environment.ProcessPath);
	}

	/// <summary>
	/// Sets <see cref="FileModelDto.IsFavorite" /> value.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSetFavorite))]
	internal Task SetFavorite(FileModelDto? dto)
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

		_viewLauncher.ConfigureFavoritesWindow(
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
	internal async Task ShowFolderContents(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.IsEmpty())
		{
			ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		FileModelDto[] openedFiles = [.. files.Where(IsOpened)];

		if (!await TryCloseOpenedFilesAsync(openedFiles).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Show file contents in a folder");

		await _entityEncryption
			.ShowFolderContentsAsync(dto)
			.ConfigureAwait(true);

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Displays the add object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanAdd))]
	private async Task Add(FolderModelDto? parent)
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
		if (RightSideSheetContent == RightSideSheetContentType.None)
		{
			return;
		}

		if (RightSideSheetContent == RightSideSheetContentType.CopyHistory)
		{
			if (CopyHistorySettings.Items.Count == 0 || !await _dialogService
				.RequestYesCancelDialogAsync($"{Strings.Clear}?")
				.ConfigureAwait(false))
			{
				return;
			}

			ClearCopyHistory();
		}
		else if (RightSideSheetContent == RightSideSheetContentType.ExecutingFiles)
		{
			if (ExecutingFiles.Count == 0 || !await _dialogService
				.RequestYesCancelDialogAsync($"{Strings.Clear}?")
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
	private void CloseOpenedFile(FileModelDto? dto)
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
	private Task CopyContentByContextMenu(FileModelDto? dto)
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
	private void CopyName(ExplorerModelBaseDto? dto)
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

			FolderModelDto[] parents = [.. dto
				.GetAllParents()
				.Reverse()];

			if (FindLastContainer(container, parents)?.ContainerFromItem(dto) is not TemplatedControl item)
			{
				return;
			}

			_exceptionHandler.Watch(BrushExtensions.ApplyLimeGreenColorAnimation(() => item.Background as Brush));
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
	private async Task Delete(ExplorerModelBaseDto? dto)
	{
		ExplorerModelBaseDto? toBeDeleted = dto ?? SelectedObject;

		if (toBeDeleted is null)
		{
			return;
		}

		_logger.LogInformation("Deleting an object using a dialog");

		bool isopened = dto is FileModelDto file && file.IsOpened();

		if (!await _dialogService
			.RequestYesNoDialogAsync($@"{(isopened ? Strings.CloseTheFileAndDelete : Strings.Delete)} ""{toBeDeleted.Name}""?")
			.ConfigureAwait(true))
		{
			return;
		}

		// The file is closed next.
		await DeleteAsync(toBeDeleted).ConfigureAwait(false);
	}

	/// <inheritdoc cref="EditingFilesViewModel.OpenInEditor" />
	[RelayCommand(CanExecute = nameof(CanBeEditedOrExecuted))]
	private async Task EditFile(FileModelDto? dto)
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
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name))}");
	}

	/// <summary>
	/// Displays the rename object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanRename))]
	private async Task Rename(ExplorerModelBaseDto? dto)
	{
		ExplorerModelBaseDto? toBeRenamed = dto ?? SelectedObject;

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
	/// Controls the display of the copy history in right side sheet.
	/// </summary>
	[RelayCommand]
	private void ShowCopyHistory() => SwitchRightSideSheetContent(RightSideSheetContentType.CopyHistory);

	/// <summary>
	/// Controls the display of the executing files in right side sheet.
	/// </summary>
	[RelayCommand]
	private void ShowExecutingFiles()
	{
		if (RightSideSheetContent == RightSideSheetContentType.CopyHistory)
		{
			SaveCopyHistory();
		}

		SwitchRightSideSheetContent(RightSideSheetContentType.ExecutingFiles);
	}

	/// <inheritdoc cref="IEntityEncryption.ShowFileContentsAsync" />
	[RelayCommand(CanExecute = nameof(CanShowFileContents))]
	private Task ShowFileContents(FileModelDto? dto)
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
	private async Task ShowHotkeysEditor(FileModelDto? dto)
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
			.EditHotkeysAsync(dto.Hotkeys.ToCodeMaskPairs())
			.ConfigureAwait(false);

		if (result.IsSaved)
		{
			await OverwriteFileHotkeysAsync(dto, result.NewHotkeys).ConfigureAwait(false);
		}

		if (!_settingsManager
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
	private void ShowProperties(ExplorerModelBaseDto? dto)
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

		await HandleChangeSettingsAsync(
			result.IsSaved,
			result.Settings).ConfigureAwait(false);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IClipboardLogService" />
	private readonly IClipboardLogService _clipboardLog;

	/// <inheritdoc cref="IClipboardLogPersistenceCoordinator" />
	private readonly IClipboardLogPersistenceCoordinator _clipboardLogPersistence;

	/// <inheritdoc cref="IDataExchangeService" />
	private readonly IDataExchangeService _dataExchange;

	/// <inheritdoc cref="IHierarchyEditor" />
	private readonly IHierarchyEditor _hierarchyEditor;

	/// <summary>
	/// Mapper.
	/// </summary>
	private readonly IMapper _mapper;

	/// <inheritdoc cref="IProcessUtils" />
	private readonly IProcessUtils _processUtils;

	/// <inheritdoc cref="IEntityPropertyWriter" />
	private readonly IEntityPropertyWriter _propertyWriter;

	/// <inheritdoc cref="EditingFilesViewModel" />
	private EditingFilesViewModel? _editingFiles;
	#endregion

	#region Constructors
	public EditorViewModel(
		Application app,
		IAppSettingsManager settingsManager,
		IClipboardAccessor clipboard,
		IClipboardLogService clipboardLog,
		IClipboardLogPersistenceCoordinator clipboardLogPersistence,
		IDataExchangeService dataExchange,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IDispatcherAccessor dispatcher,
		IEntityEncryption entityEncryption,
		IEntityPropertyWriter propertyWriter,
		IExecutionEngine executionEngine,
		IHierarchyEditor hierarchyEditor,
		ILogger logger,
		IMapper mapper,
		IMessenger messenger,
		IProcessUtils processUtils,
		ITaskExceptionHandler exceptionHandler,
		IViewLauncher viewLauncher,
		Lazy<IKeyboardInputHook> keyboardInputHook) : base(
			app,
			settingsManager,
			clipboard,
			dbAccess,
			dialogService,
			dispatcher,
			entityEncryption,
			executionEngine,
			logger,
			messenger,
			exceptionHandler,
			viewLauncher,
			keyboardInputHook)
	{
		_clipboardLog = clipboardLog;

		_clipboardLogPersistence = clipboardLogPersistence;

		_dataExchange = dataExchange;

		_hierarchyEditor = hierarchyEditor;

		_mapper = mapper;

		_processUtils = processUtils;

		_propertyWriter = propertyWriter;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void AddHierarchy(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		Hierarchy.AddRange(hierarchy);

		CountHierarchy();
	}

	/// <summary>
	/// Performs initialization.
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

		CopyHistorySettings.AddItems(copyHistorySettings.Items, Hierarchy);

		if (CopyHistorySettings.Items.Count > 0)
		{
			CopyHistorySettings.SelectedItemId = copyHistorySettings.SelectedItemId;
		}

		IsInitialized = true;
	}

	/// <summary>
	/// Overrites hotkeys of a file.
	/// </summary>
	public async Task<OverwriteHotkeysResult> OverwriteFileHotkeysAsync(
		FileModelDto dto,
		CodeMaskPair[] newHotkeys,
		CancellationToken token = default)
	{
		IEqualityComparer<HotkeyModelDto> comparer = Equality.Of<HotkeyModelDto>()
			.By(x => x.Code)
			.AndBy(x => x.Mask);

		HotkeyModelDto[] temp = [.. newHotkeys.ToHotkeyModelsDto()];

		if (dto
			.Hotkeys
			.SequenceEqual(temp, comparer))
		{
			return OverwriteHotkeysResult.SameHotkeys;
		}

		if (temp.IsNotEmpty() && Hierarchy.FindFileBy(x => x.Hotkeys.SequenceEqual(temp, comparer)) is { } existed)
		{
			string sequence = newHotkeys.GetHotkeysPresentation();

			ShowWarningSnackbar(
				$@"{string.Format(Strings.HotkeysAlreadyAssignedFor, sequence)} ""{existed.Name}""");

			return OverwriteHotkeysResult.AlreadyInUse;
		}

		try
		{
			if (dto.Hotkeys.Count > 0)
			{
				dto
					.Hotkeys
					.Clear();

				await _dbAccess
					.DeleteHotkeysAsync(dto.Id, token)
					.ConfigureAwait(false);
			}

			if (newHotkeys.IsEmpty())
			{
				return OverwriteHotkeysResult.EmptySequence;
			}

			try
			{
				HotkeyModel[] createdHotkeys = await _dbAccess
					.AddHotkeysAsync(dto.Id, newHotkeys, token)
					.ConfigureAwait(false);

				HotkeyModelDto[] mapped = _mapper.Map<HotkeyModel[], HotkeyModelDto[]>(createdHotkeys);

				dto
					.Hotkeys
					.AddRange(mapped);

				return OverwriteHotkeysResult.Rewritten;
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);

				return OverwriteHotkeysResult.ExceptionThrown;
			}
		}
		finally
		{
			dto.SetHotkeysToolTip();
		}
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
	/// Sets the <see cref="ExplorerModelBaseDto.IsSelected" /> to <c>True</c> and <see cref="SelectedObject" /> from <paramref name="selected"/>.
	/// </summary>
	public void SetSelectedObject(ExplorerModelBaseDto selected)
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

		FolderModelDto[] parents = [.. found
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

		await BrushExtensions.ApplyLimeGreenColorAnimation(
			() => item.Background as Brush,
			token).ConfigureAwait(false);
	}

	/// <summary>
	/// Adds <see cref="ExplorerModelBase" /> to the database and <see cref="ExplorerModelBaseDto" /> to the <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	internal async Task<ExplorerModelBaseDto?> AddAsync(
		string name,
		EntityType entityType,
		FolderModelDto? parent,
		CancellationToken token = default)
	{
		ExplorerModelBaseDto? dto = await _hierarchyEditor
			.AddAsync(name, entityType, parent, Hierarchy, token)
			.ConfigureAwait(false);

		if (dto is not null)
		{
			CountHierarchy();
		}

		return dto;
	}

	/// <summary>
	/// Closes editing and executing files.
	/// </summary>
	internal void CloseFiles(
		IEnumerable<FileModelDto> editingFiles,
		IEnumerable<FileModelDto> executingFiles)
	{
		foreach (FileModelDto file in editingFiles)
		{
			CloseEditingFile(file);
		}

		foreach (FileModelDto file in executingFiles)
		{
			CloseExecutingFile(file);
		}
	}

	/// <summary>
	/// Deletes an object from the database and from <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	internal async Task<bool> DeleteAsync(
		ExplorerModelBaseDto dto,
		CancellationToken token = default)
	{
		if (!await _hierarchyEditor
			.DeleteAsync(dto, Hierarchy, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		if (dto is FileModelDto file)
		{
			CloseFile(file);

			RemoveFromCopyHistory(file);
		}

		CountHierarchy();

		return true;
	}

	/// <summary>
	/// Expands or collapses all folders in <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	/// <remarks>
	/// Changes to the <see cref="ExplorerModelBaseDto.IsExpanded" /> property of folders are saved to the database
	/// using the <see cref="Receive(FolderExpandedChangedMessage)" /> message handler.
	/// </remarks>
	internal Task ExpandCollapseAllFoldersAsync(bool isExpanded)
	{
		if (!isExpanded)
		{
			ResetSelectedObject();
		}

		FolderModelDto[] folders = [.. Hierarchy.GetFoldersBy(x => x.IsExpanded != isExpanded)];

		if (folders.IsEmpty())
		{
			return Task.CompletedTask;
		}

		return folders.ForEachAsync(x => x.IsExpanded = isExpanded, Environment.ProcessorCount);
	}

	/// <summary>
	/// Handles changing application settings.
	/// </summary>
	internal async Task HandleChangeSettingsAsync(
		bool isSave,
		AppSettings settings,
		CancellationToken token = default)
	{
		// Captured before overwrite so we can detect a persistence ON -> OFF transition below.
		bool wasPersistingClipboard = _settingsManager
			.Settings
			.PersistClipboardHistory;

		if (isSave)
		{
			_settingsManager.OverwriteSettings(settings);

			_settingsManager.SaveSettingsInFile();
		}
		else
		{
			_settingsManager.ApplyMaterialTheme();
		}

		if (!isSave)
		{
			return;
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
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="HideFileContentsCommand" />.
	/// </summary>
	private static bool CanHideFileContents(FileModelDto? dto)
	{
		return dto is not null && dto.EncryptionStatus == EncryptionStatus.Decrypted;
	}

	/// <summary>
	/// Validates <see cref="HideFolderContentsCommand" />.
	/// </summary>
	private static bool CanHideFolderContents(FolderModelDto? dto)
	{
		return dto?.EncryptionStatus.IsNotDefault() == true
			&& dto.AnyChild(x => x.EncryptionStatus == EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// Returns a sequence with information on the properties of an object.
	/// </summary>
	private static IEnumerable<PropertyNameValuePair> GetPropertyDescriptions(ExplorerModelBaseDto dto)
	{
		const string format = "dd.MM.yyyy HH:mm:ss";

		yield return new(
			Strings.Type,
			dto.EntityType switch
			{
				EntityType.Folder => Strings.Folder,
				EntityType.File => Strings.File,
				EntityType.DataSet => Strings.Dataset,
				_ => throw new NotImplementedException()
			});

		yield return new(Strings.Name, dto.Name);

		yield return new(Strings.Created, dto.CreatedDate.ToString(format));

		yield return new(Strings.Updated, dto.UpdatedDate.ToString(format));
	}

	/// <inheritdoc cref="FileModelDto.IsOpened" />
	private static bool IsOpened(FileModelDto dto) => dto.IsOpened();

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
	private bool CanBeEditedOrExecuted(FileModelDto? dto)
	{
		return !IsActionInProgress
			&& dto is not null
			&& !IsOpened(dto);
	}

	/// <summary>
	/// Validates <see cref="ChangePasswordCommand" />.
	/// </summary>
	private bool CanChangePassword(FolderModelDto? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto?.IsPasswordKeeper() == true;
	}

	/// <summary>
	/// Validates <see cref="CopyContentByContextMenuCommand" />.
	/// </summary>
	private bool CanCopyContent(FileModelDto? dto) => dto?.IsOpened() == false && !IsActionInProgress;

	/// <summary>
	/// Validates <see cref="DecryptFolderCommand" />.
	/// </summary>
	private bool CanDecryptFolder(FolderModelDto? dto)
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
	private bool CanDelete(ExplorerModelBaseDto? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& (dto is not null || SelectedObject is not null);
	}

	/// <summary>
	/// Validates <see cref="EncryptFolderCommand" />.
	/// </summary>
	private bool CanEncryptFolder(FolderModelDto? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto?.IsPasswordKeeper() == false
			&& !dto.AnyParent(x => x.IsPasswordKeeper())
			&& !dto.AnyChild(x => x.EncryptionStatus != EncryptionStatus.None);
	}

	/// <summary>
	/// Validates <see cref="ExportCommand" />.
	/// </summary>
	private bool CanExport() => !IsActionInProgress && Hierarchy.Count > 0;

	/// <summary>
	/// Validates <see cref="HideAllFileContentsCommand" />.
	/// </summary>
	private bool CanHideAllFiles() => Hierarchy.ContainsBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted);

	/// <summary>
	/// Validates <see cref="ImportCommand" />.
	/// </summary>
	private bool CanImport() => !IsReadOnly && !IsActionInProgress;

	/// <summary>
	/// Validates <see cref="RenameCommand" />.
	/// </summary>
	private bool CanRename(ExplorerModelBaseDto? dto)
	{
		return !IsReadOnly
			&& !IsActionInProgress
			&& dto is not null
			&& (dto is not FileModelDto file || !file.IsOpened());
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
	private bool CanShowFileContents(FileModelDto? dto)
	{
		return !IsActionInProgress
			&& dto is not null
			&& dto.EncryptionStatus == EncryptionStatus.Encrypted;
	}

	/// <summary>
	/// Validates <see cref="ShowFileContentsCommand" />.
	/// </summary>
	private bool CanShowFolderContents(FolderModelDto? dto)
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
	/// Closes editing file.
	/// </summary>
	private void CloseEditingFile(FileModelDto file)
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

	/// <summary>
	/// Closes file that is being edited or executed;
	/// </summary>
	private void CloseFile(FileModelDto file)
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
	/// Counts the number of objects in <see cref="ViewModelBase.Hierarchy" />.
	/// </summary>
	private void CountHierarchy() => BottomLeftCornerInfo = Hierarchy.GetCount().AsString();

	/// <summary>
	/// Tries to remove value from copy history.
	/// </summary>
	private void RemoveFromCopyHistory(FileModelDto file)
	{
		CopyHistorySettings
			.Items
			.Remove(file.Id);

		_copyHistory?.Remove(file);
	}

	/// <inheritdoc cref="IEntityEncryption.ShowFileContentsAsync" />
	private async Task<bool> ShowFileContentsAsync(FileModelDto dto)
	{
		_logger.LogInformation("Show file contents");

		if (!await _entityEncryption
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
	private void SwitchRightSideSheetContent(RightSideSheetContentType type)
	{
		if (RightSideSheetContent == type)
		{
			IsRightSideSheetOpened = false;

			return;
		}

		_logger.LogInformation($"Show {type switch
		{
			RightSideSheetContentType.CopyHistory => "copy history",
			RightSideSheetContentType.ExecutingFiles => "executing files",
			_ => "unknown"
		}}");

		RightSideSheetContent = type;

		IsRightSideSheetOpened = true;
	}

	/// <summary>
	/// Tries to close editing or executing files if any.
	/// </summary>
	private async Task<bool> TryCloseOpenedFilesAsync(
		FileModelDto[] openedFiles,
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
	#endregion
}
