using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Comparation;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using DialogHostAvalonia;
using Entities.Abstract;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using Material.Styles.Controls;
using Microsoft.Data.Sqlite;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Properties;
using SharpHook;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BrushExtensions = DataOrganizer.Extensions.BrushExtensions;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="EditorWindow" />.
/// </summary>
public partial class EditorViewModel : ViewModelBase, INavigationColumnViewModel
{
	#region Properties
	/// <inheritdoc cref="EditFilesView" />
	public EditFilesView EditFiles { get; }
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Information in the lower left corner.
	/// </summary>
	[ObservableProperty]
	private string? _bottomLeftCornerInfo;

	/// <summary>
	/// Controls the progress bar for an action.
	/// </summary>
	[ObservableProperty]
	private bool _isActionInProgress;

	/// <summary>
	/// Controls the display of the <see cref="NavigationDrawer" />.
	/// </summary>
	[ObservableProperty]
	private bool _isLeftDrawerOpened;

	/// <summary>
	/// Read-only mode.
	/// </summary>
	[ObservableProperty]
	private bool _isReadOnly;

	/// <summary>
	/// Returns <c>True</c> when right side sheet should be opened.
	/// </summary>
	[ObservableProperty]
	private bool _isRightSideSheetOpened;

	/// <inheritdoc cref="INavigationColumnViewModel.NavigationColumnWidth" />
	[ObservableProperty]
	private GridLength _navigationColumnWidth;

	/// <summary>
	/// The content of the right side sheet.
	/// </summary>
	[ObservableProperty]
	private object? _rightSideSheetContent;

	/// <inheritdoc cref="EditorRightSideSheetContentType" />
	[ObservableProperty]
	private EditorRightSideSheetContentType _rightSideSheetContentType;

	/// <summary>
	/// The selected object in <see cref="TreeView" /> from <see cref="Hierarchy" />.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
	[NotifyCanExecuteChangedFor(nameof(RenameCommand))]
	[NotifyCanExecuteChangedFor(nameof(ResetSelectedObjectCommand))]
	private ExplorerModelBaseDto? _selectedObject;

	/// <summary>
	/// Window width.
	/// </summary>
	[ObservableProperty]
	private double _viewWidth;
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

		RenameCommand.NotifyCanExecuteChanged();

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

		RightSideSheetContentType = EditorRightSideSheetContentType.None;

		ClearExecutedFilesView();

		SaveCopyHistory();

		RightSideSheetContent = null;
	}

	/// <summary>
	/// Called when <see cref="SelectedObject" /> changes.
	/// </summary>
	partial void OnSelectedObjectChanging(
		ExplorerModelBaseDto? oldValue,
		ExplorerModelBaseDto? newValue)
	{
		if (IsReadOnly || _app.IsAnyWindow<EditorWindow>(x => !x.IsLoaded || !x.IsVisible))
		{
			return;
		}

		// When an object is removed from a collection, its selection is reset and its existence must be checked
		// to ensure that no attempt is made to save properties to the database for a non-existent object.
		if (oldValue is not null && Hierarchy.ContainsId(oldValue.Id))
		{
			_ = UpdateIsSelectedInDatabaseAsync(oldValue);
		}

		if (newValue is null)
		{
			return;
		}

		_ = UpdateIsSelectedInDatabaseAsync(newValue);
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
	[RelayCommand(CanExecute = nameof(CanExecuteChangePassword))]
	public async Task ChangePassword(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (!await TryCloseEditedExecutedFilesAsync(files).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Change password of the folder");

		await _entityEcryption
			.ChangePasswordAsync(dto)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Closes a file executed in the operating system.
	/// </summary>
	[RelayCommand]
	public void CloseExecutedFile(FileModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		_logger.LogInformation($"Closing an executed file in operating system:{dto.GetPropertyValues(
			true,
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name),
			nameof(FileModelDto.EntityType))}");

		ExecutedFiles.Remove(dto);

		CloseAllExecutedFilesCommand.NotifyCanExecuteChanged();

		dto.IsExecuted = false;

		_ = _executionEngine.CloseAsync(dto.Id);
	}

	/// <summary>
	/// Decrypts files in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteDecryptFolder))]
	public async Task DecryptFolder(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.Length == 0)
		{
			ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		if (!await TryCloseEditedExecutedFilesAsync(files).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Decrypt files in a folder");

		await _entityEcryption
			.DecryptFolderAsync(dto, files)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Encrypts files in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteEncryptFolder))]
	public async Task EncryptFolder(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.Length == 0)
		{
			ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		if (!await TryCloseEditedExecutedFilesAsync(files).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Encrypt files in a folder");

		await _entityEcryption
			.EncryptFolderAsync(dto, files)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Executes the file in the operating system.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanBeEditedOrExecuted))]
	public async Task ExecuteFile(FileModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		if (_executionEngine.IsExecuted(dto.Id))
		{
			_logger.LogWarning($"The file is already executed in the operating system:{dto.GetPropertyValues(
				true,
				nameof(FileModelDto.Id),
				nameof(FileModelDto.Name),
				nameof(FileModelDto.EntityType))}");

			return;
		}

		_logger.LogInformation($"The file needs to be executed in the operating system:{dto.GetPropertyValues(
			true,
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name),
			nameof(FileModelDto.EntityType))}");

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

			byte[]? decryptedContents = _entityEcryption.DecryptSessionContents(
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
			ViewModel = this
		};

		if (!await _executionEngine
			.ExecuteAsync(parameters)
			.ConfigureAwait(false))
		{
			return;
		}

		dto.IsExecuted = true;

		ExecutedFiles.Add(dto);

		_dispatcher.Post(CloseAllExecutedFilesCommand.NotifyCanExecuteChanged);
	}

	/// <summary>
	/// Exits the application.
	/// </summary>
	[RelayCommand]
	public void Exit(Window? window)
	{
		IsShutdown = true;

		window?.Close();
	}

	/// <summary>
	/// Hides all file contents.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteHideAllFiles))]
	public async Task HideAllFileContents()
	{
		FileModelDto[] openedFiles = [.. Hierarchy.GetFilesBy(x => x.IsEdited || x.IsExecuted)];

		if (openedFiles.Length > 0 && !await TryCloseEditedExecutedFilesAsync(openedFiles).ConfigureAwait(true))
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
	[RelayCommand(CanExecute = nameof(CanExecuteFileContents))]
	public async Task HideFileContents(FileModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		if (dto.IsEdited || dto.IsExecuted)
		{
			if (!await _dialogService
				.RequestUserCloseFilesAsync()
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

	/// <inheritdoc cref="IEntityEcryption.HideFolderContents" />
	[RelayCommand(CanExecute = nameof(CanExecuteHideFolderContents))]
	public async Task HideFolderContents(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		if (!await TryCloseEditedExecutedFilesAsync([.. dto.Children.GetFiles()]).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Hide files in a folder");

		_entityEcryption.HideFolderContents(dto, Hierarchy);

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Resets the <see cref="SelectedObject" />.
	/// </summary>
	/// <remarks>
	/// Change to the <see cref="ExplorerModelBaseDto.IsSelected" /> property is saved to the database
	/// using the <see cref="OnSelectedObjectChanging(ExplorerModelBaseDto?, ExplorerModelBaseDto?)" /> method.
	/// </remarks>
	[RelayCommand(CanExecute = nameof(IsSelectedObjectNotNull))]
	public void ResetSelectedObject()
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
	public void RestartApplication(Window? window)
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	public Task SetFavorite(FileModelDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		dto.IsFavorite = !dto.IsFavorite;

		return UpdateFileIsFavoriteInDatabaseAsync(dto);
	}

	/// <summary>
	/// Displays the "Favorites" window.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteShowFavorites))]
	public void ShowFavorites(EditorWindow? window)
	{
		IsShutdown = false;

		window?.Close();

		_viewLauncher.ConfigureFavoritesWindow(
			Hierarchy,
			[.. EditFiles.ViewModel.EditFiles],
			ExecutedFiles).Show();

		Hierarchy
			.GetFilesBy(x => x.IsEdited)
			.ForEach(EditFiles.ViewModel.CloseEditor);
	}

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteShowFolderContents))]
	public async Task ShowFolderContents(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		FileModelDto[] files = [.. dto
			.Children
			.GetFiles()];

		if (files.Length == 0)
		{
			ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		if (!await TryCloseEditedExecutedFilesAsync(files).ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Show file contents in a folder");

		await _entityEcryption
			.ShowFolderContentsAsync(dto)
			.ConfigureAwait(true);

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Displays the hotkey editor.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	public Task ShowHotkeysEditor(FileModelDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation("Show hotkeys editor");

		if (_keyboardInputHook.IsRunning)
		{
			_keyboardInputHook.StopTracking();
		}

		HotkeysEditorView view = _viewFactory.CreateUserControl<HotkeysEditorView>();

		view
			.ViewModel
			.Buffer
			.AddRange(dto.Hotkeys.ToCodeMaskPairs());

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return Task.CompletedTask;
		}

		return DialogHost.Show(view, Dialog_Closing);

		void Dialog_Closing(object sender, DialogClosingEventArgs e)
		{
			_ = HandleChangeHotkeysAsync(view.ViewModel, dto);
		}
	}

	/// <summary>
	/// Shows application settings.
	/// </summary>
	[RelayCommand]
	public Task ShowSettings()
	{
		IsLeftDrawerOpened = false;

		_logger.LogInformation("Show settings");

		SettingsView view = _viewFactory.CreateUserControl<SettingsView>();

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return Task.CompletedTask;
		}

		return DialogHost.Show(view, Dialog_Closing);

		void Dialog_Closing(object sender, DialogClosingEventArgs e)
		{
			HandleChangeSettings(
				view.ViewModel.IsSaved,
				view.ViewModel.CurrentSettings);
		}
	}

	/// <summary>
	/// Displays the add object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private async Task Add(FolderModelDto? parent)
	{
		_logger.LogInformation("Adding an object using a dialog");

		EntityCreationView view = _viewFactory.CreateUserControl<EntityCreationView>();

		_ = DialogHost.Show(view);

		try
		{
			if (!await view
				.ViewModel
				.GetResultAsync()
				.ConfigureAwait(false))
			{
				return;
			}

			EntityType entityType = view.ViewModel switch
			{
				{ IsFolderSelected: true } => EntityType.Folder,
				{ IsFileSelected: true } => EntityType.File,
				{ IsDatasetSelected: true } => EntityType.DataSet,
				_ => throw new NotImplementedException()
			};

			await AddAsync(
				view.ViewModel.Name,
				entityType,
				parent).ConfigureAwait(false);
		}
		finally
		{
			view
				.ViewModel
				.SaveSettingsInFile();
		}
	}

	/// <summary>
	/// Closes all files executed in the operating system.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteCloseAllExecutedFiles))]
	private void CloseAllExecutedFiles() => ExecutedFiles.ToArray().ForEach(CloseExecutedFile);

	/// <summary>
	/// Collapses all folders in <see cref="Hierarchy" />.
	/// </summary>
	[RelayCommand]
	private Task CollapseAllFolders() => ExpandCollapseAllFoldersAsync(false);

	/// <inheritdoc cref="CopyContentViewModelBase.CopyContentAsync" />
	[RelayCommand]
	private async Task CopyContent(FileModelDto? dto)
	{
		if (dto is null
			|| _app.FindWindow<EditorWindow>() is not { } window
			|| window.FindLogicalChild<TreeView>() is not { } container)
		{
			return;
		}

		await CopyContentAsync(dto, container).ConfigureAwait(true);

		if (RightSideSheetContentType != EditorRightSideSheetContentType.CopyHistory)
		{
			return;
		}

		DisplayCopyHistory();

		if (RightSideSheetContent is not CopyHistoryView view)
		{
			return;
		}

		view
			.ViewModel
			.SetSelectedItem(dto);
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
				|| window.Clipboard is not { } clipboard
				|| window.FindLogicalChild<TreeView>() is not { } container)
			{
				return;
			}

			_ = clipboard.SetTextAsync(dto.Name);

			if (container.ContainerFromItem(dto) is not TemplatedControl item)
			{
				return;
			}

			_ = BrushExtensions.ApplyLimeGreenColorAnimation(() => item.Background as Brush);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Displays the delete object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteDelete))]
	private async Task Delete(ExplorerModelBaseDto? dto)
	{
		ExplorerModelBaseDto? toBeDeleted = dto ?? SelectedObject;

		if (toBeDeleted is null)
		{
			return;
		}

		_logger.LogInformation("Deleting an object using a dialog");

		YesNoCancelBox view = _viewFactory.CreateUserControl<YesNoCancelBox>();

		view
			.ViewModel
			.Text = $@"{Strings.Delete} ""{toBeDeleted.Name}""?";

		_ = DialogHost.Show(view);

		YesNoCancelResult result = await view
			.ViewModel
			.GetResultAsync(YesNoCancelVariant.YesNo)
			.ConfigureAwait(false);

		if (result != YesNoCancelResult.Yes)
		{
			return;
		}

		await DeleteAsync(toBeDeleted).ConfigureAwait(false);
	}

	/// <inheritdoc cref="EditFilesViewModel.OpenInEditor(FileModelDto)" />
	[RelayCommand(CanExecute = nameof(CanBeEditedOrExecuted))]
	private void EditFile(FileModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		EditFiles
			.ViewModel
			.OpenInEditor(dto);
	}

	/// <summary>
	/// Expands all folders in <see cref="Hierarchy" />.
	/// </summary>
	[RelayCommand]
	private Task ExpandAllFolders() => ExpandCollapseAllFoldersAsync(true);

	/// <summary>
	/// Exports data.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteExport))]
	private async Task Export()
	{
		// TODO: Test
		FilePickerSaveOptions options = new()
		{
			DefaultExtension = IFileSystemEnrtyPicker.JsonExt.TrimStart('.'),
			FileTypeChoices = IFileSystemEnrtyPicker.ImportExportFilePickerTypes,
			ShowOverwritePrompt = true,
			SuggestedFileName = AppUtils.AppNameInOneWord,
			Title = Strings.SaveAs
		};

		if (await _picker
			.SaveFileAsync<EditorWindow>(options)
			.ConfigureAwait(false) is not { } filePath)
		{
			return;
		}

		try
		{
			IsActionInProgress = true;

			switch (Path.GetExtension(filePath))
			{
				case IFileSystemEnrtyPicker.JsonExt:
					break;

				case IFileSystemEnrtyPicker.XmlExt:
					break;

				case AppUtils.SQLiteExtension:
					_dbAccess.BackupSqliteDatabase(_dbAccess.GetDbFilePath(), filePath);

					SqliteConnection.ClearAllPools();
					break;

				default:
					throw new NotImplementedException();
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			IsActionInProgress = false;
		}
	}

	/// <summary>
	/// Imports data.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private async Task Import()
	{
		// TODO: Test
		FileModelDto[] openedFiles = [.. Hierarchy.GetFilesBy(x => x.IsEdited || x.IsExecuted)];

		if (openedFiles.Length > 0 && !await TryCloseEditedExecutedFilesAsync(openedFiles).ConfigureAwait(false))
		{
			return;
		}

		if (_app
			.FindWindow<EditorWindow>()?
			.StorageProvider is not { } storageProvider)
		{
			return;
		}

		const string jsonExt = ".json";

		const string xmlExt = ".xml";

		FilePickerFileType[] types =
		[
			new("JSON File")
			{
				Patterns = [$"*{jsonExt}"],
				MimeTypes = ["application/json"]
			},
			new("XML File")
			{
				Patterns = [$"*{xmlExt}"],
				MimeTypes = ["application/xml"]
			},
			new("SQLite Database File")
			{
				Patterns = [$"*{AppUtils.SQLiteExtension}"],
				MimeTypes = ["application/x-sqlite3"]
			}
		];

		FilePickerOpenOptions options = new()
		{
			AllowMultiple = false,
			FileTypeFilter = types,
			Title = Strings.Select
		};

		IReadOnlyList<IStorageFile> files = await storageProvider
			.OpenFilePickerAsync(options)
			.ConfigureAwait(false);

		if (files.Count == 0)
		{
			return;
		}

		try
		{
			IsActionInProgress = true;

			string filePath = files[0]
				.Path
				.AbsolutePath;

			;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			IsActionInProgress = false;
		}
	}

	/// <summary>
	/// Opens a file context menu.
	/// </summary>
	[RelayCommand]
	private void OpenFileContextMenu(Control? container)
	{
		if (container?.ContextFlyout is not { } flyout)
		{
			return;
		}

		flyout.ShowAt(container);

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
	[RelayCommand(CanExecute = nameof(CanExecuteRename))]
	private async Task Rename(ExplorerModelBaseDto? dto)
	{
		ExplorerModelBaseDto? toBeRenamed = dto ?? SelectedObject;

		if (toBeRenamed is null)
		{
			return;
		}

		_logger.LogInformation("Renaming an object using dialog");

		KeyValueInputView view = _viewFactory.CreateUserControl<KeyValueInputView>();

		view.ViewModel.Initialize(
			defaultButtonText: Strings.Rename,
			key: toBeRenamed.Name,
			keyHint: Strings.Name);

		_ = DialogHost.Show(view);

		if (!await view
			.ViewModel
			.GetResultAsync()
			.ConfigureAwait(false) || view.ViewModel.Key is not { } newName)
		{
			return;
		}

		await RenameAsync(
			toBeRenamed,
			newName,
			DateTime.Now).ConfigureAwait(false);
	}

	/// <summary>
	/// Controls the display of the copy history in right side sheet.
	/// </summary>
	[RelayCommand]
	private void ShowCopyHistory()
	{
		if (RightSideSheetContentType != EditorRightSideSheetContentType.CopyHistory)
		{
			ClearExecutedFilesView();

			DisplayCopyHistory();
		}
		else
		{
			IsRightSideSheetOpened = false;
		}
	}

	/// <summary>
	/// Controls the display of the executed files in right side sheet.
	/// </summary>
	[RelayCommand]
	private void ShowExecutedFiles()
	{
		if (RightSideSheetContentType != EditorRightSideSheetContentType.ExecutedFiles)
		{
			SaveCopyHistory();

			DisplayExecutedFiles();
		}
		else
		{
			IsRightSideSheetOpened = false;
		}
	}

	/// <inheritdoc cref="IEntityEcryption.ShowFileContentsAsync" />
	[RelayCommand(CanExecute = nameof(CanExecuteShowFileContents))]
	private async Task ShowFileContents(FileModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		_logger.LogInformation("Show file contents");

		await _entityEcryption
			.ShowFileContentsAsync(dto)
			.ConfigureAwait(true);

		HideAllFileContentsCommand.NotifyCanExecuteChanged();
	}

	/// <inheritdoc cref="ViewModelBase.ShowInEditorAsync" />
	[RelayCommand]
	private void ShowInList(Guid id) => _ = ShowInEditorAsync(_app.FindWindow<EditorWindow>(), id);

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

		PropertiesView view = _viewFactory.CreateUserControl<PropertiesView>();

		view
			.ViewModel
			.Properties
			.AddRange(GetPropertyDescriptions(dto));

		DialogHost.Show(view);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IExecutionEngine" />
	private readonly IExecutionEngine _executionEngine;

	/// <summary>
	/// Mapper.
	/// </summary>
	private readonly IMapper _mapper;

	/// <inheritdoc cref="IFileSystemEnrtyPicker" />
	private readonly IFileSystemEnrtyPicker _picker;

	/// <inheritdoc cref="IProcessUtils" />
	private readonly IProcessUtils _processUtils;
	#endregion

	#region Constructors
	public EditorViewModel(
		Application app,
		IAppSettingsManager settingsManager,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IDispatcher dispatcher,
		IEncryptionService encryption,
		IEntityEcryption entityEcryption,
		IEventSimulator eventSimulator,
		IExecutionEngine executionEngine,
		IFileSystemEnrtyPicker picker,
		IKeyboardInputHook keyboardInputHook,
		ILogger logger,
		IMapper mapper,
		IProcessUtils processUtils,
		IViewFactory viewFactory,
		IViewLauncher viewLauncher
		) : base(
			app,
			settingsManager,
			dbAccess,
			dialogService,
			dispatcher,
			encryption,
			entityEcryption,
			eventSimulator,
			keyboardInputHook,
			logger,
			viewFactory,
			viewLauncher)
	{
		_executionEngine = executionEngine;

		_mapper = mapper;

		_picker = picker;

		_processUtils = processUtils;

		EditFiles = viewFactory.CreateUserControl<EditFilesView>();

		FolderModelDto.IsExpandedChanged += Folder_IsExpandedChanged;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="FolderModelDto.IsExpandedChanged" /> event handler of <see cref="FolderModelDto" />.
	/// </summary>
	/// <remarks>
	/// There was no way to track the expand/collapse events of <see cref="TreeViewItem" /> in Xaml,
	/// so I had to use a global event to persist the changes to the database in one place.
	/// </remarks>
	private void Folder_IsExpandedChanged(object? sender, FolderModelDto e)
	{
		if (IsReadOnly)
		{
			return;
		}

		_ = UpdateFolderIsExpandedInDatabaseAsync(e);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Adds <see cref="ExplorerModelBase" /> to the database and <see cref="ExplorerModelBaseDto" /> to the <see cref="Hierarchy" />.
	/// </summary>
	public async Task<ExplorerModelBaseDto?> AddAsync(
		string name,
		EntityType entityType,
		FolderModelDto? parent,
		CancellationToken token = default)
	{
		_logger.LogInformation($"Adding a {entityType switch
		{
			EntityType.Folder => "folder",
			EntityType.File => "file",
			EntityType.DataSet => "dataset",
			_ => throw new NotImplementedException()
		}} to the database.");

		AddEntityParameters parameters = new()
		{
			EntityType = entityType,
			Index = parent is not null ? parent.Children.Count : Hierarchy.Count,
			Name = name,
			ParentId = parent?.Id
		};

		if (await _dbAccess
			.AddEntityAsync(parameters, token)
			.ConfigureAwait(false) is not { } entity)
		{
			string errorText = $@"{Strings.FailedToAdd} ""{name}""";

			ShowErrorSnackbar(errorText);

			_logger.LogError(errorText);

			return null;
		}

		_logger.LogInformation($"The object has been added to the database:{entity.GetPropertyValues(
			true,
			nameof(ExplorerModelBase.Id),
			nameof(ExplorerModelBase.Name),
			nameof(ExplorerModelBase.EntityType),
			nameof(ExplorerModelBase.ParentId))}");

		try
		{
			ExplorerModelBaseDto dto = _mapper.Map<ExplorerModelBase, ExplorerModelBaseDto>(entity);

			dto.Parent = parent;

			GetCollectionToAdd(parent, Hierarchy).Add(dto);

			CountHierarchy();

			if (parent?.IsExpanded == false)
			{
				parent.IsExpanded = true;
			}

			string successText = $@"""{dto.Name}"" {Strings.HasBeenAdded}";

			ShowInfoSnackbar(successText);

			_logger.LogInformation(successText);

			return dto;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
	}

	/// <inheritdoc />
	public override void AddHierarchy(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		Hierarchy.AddRange(hierarchy);

		CountHierarchy();
	}

	/// <summary>
	/// Clears <see cref="ExecutedFilesView" /> in <see cref="RightSideSheetContent" />.
	/// </summary>
	public void ClearExecutedFilesView()
	{
		if (RightSideSheetContent is not ExecutedFilesView view)
		{
			return;
		}

		view
			.ViewModel
			.ExecutedFiles = null;
	}

	/// <summary>
	/// Closes edited and executed files.
	/// </summary>
	public void CloseFiles(
		IEnumerable<FileModelDto> editedFiles,
		IEnumerable<FileModelDto> executedFiles)
	{
		foreach (FileModelDto file in editedFiles)
		{
			EditFiles
				.ViewModel
				.CloseTab(file);
		}

		foreach (FileModelDto file in executedFiles)
		{
			CloseExecutedFile(file);
		}
	}

	/// <summary>
	/// Deletes an object from the database and from <see cref="Hierarchy" />.
	/// </summary>
	public async Task<bool> DeleteAsync(
		ExplorerModelBaseDto dto,
		CancellationToken token = default)
	{
		bool result = dto.EntityType switch
		{
			EntityType.Folder => await _dbAccess.DeleteFolderAsync(dto.Id, token).ConfigureAwait(false),
			_ => await _dbAccess.DeleteFileAsync(dto.Id, token).ConfigureAwait(false)
		};

		if (!result)
		{
			string errorText = $@"{Strings.FailedToDelete} ""{dto.Name}""";

			ShowErrorSnackbar(errorText);

			_logger.LogError(errorText);

			return false;
		}

		GetCollectionToDelete(dto, Hierarchy).Remove(dto);

		if (dto is FileModelDto file)
		{
			CloseFile(file);
		}

		CountHierarchy();

		string text = $@"""{dto.Name}"" {Strings.HasBeenDeleted}";

		ShowInfoSnackbar(text);

		_logger.LogInformation(text);

		return true;
	}

	/// <inheritdoc />
	public override void DisplayCopyHistory()
	{
		_logger.LogInformation($@"Show ""{nameof(CopyHistoryView)}""");

		CopyHistoryView view = _viewFactory.CreateUserControl<CopyHistoryView>();

		view.ViewModel.Initialize(
			Hierarchy.FilterFilesById(CopyHistorySettings.CopyHistory),
			CopyHistorySettings.SelectedCopyHistoryItemId);

		RightSideSheetContent = view;

		RightSideSheetContentType = EditorRightSideSheetContentType.CopyHistory;

		IsRightSideSheetOpened = true;
	}

	/// <summary>
	/// Displays <see cref="ExecutedFilesView" /> in <see cref="RightSideSheetContent" />.
	/// </summary>
	public void DisplayExecutedFiles()
	{
		_logger.LogInformation($@"Show ""{nameof(ExecutedFilesView)}""");

		ExecutedFilesView view = _viewFactory.CreateUserControl<ExecutedFilesView>();

		view
			.ViewModel
			.ExecutedFiles = ExecutedFiles;

		RightSideSheetContent = view;

		RightSideSheetContentType = EditorRightSideSheetContentType.ExecutedFiles;

		IsRightSideSheetOpened = true;
	}

	/// <summary>
	/// Expands or collapses all folders in <see cref="Hierarchy" />.
	/// </summary>
	/// <remarks>
	/// Changes to the <see cref="FolderModelDto.IsExpanded" /> property of folders are saved to the database
	/// using the <see cref="Folder_IsExpandedChanged(object?, FolderModelDto)" /> event handler.
	/// </remarks>
	public async Task ExpandCollapseAllFoldersAsync(bool isExpanded)
	{
		if (!isExpanded)
		{
			ResetSelectedObject();
		}

		FolderModelDto[] folders = [.. Hierarchy.GetFoldersBy(x => x.IsExpanded != isExpanded)];

		if (folders.Length == 0)
		{
			return;
		}

		await Task
			.WhenAll(folders.Select(x => Task.Run(() => x.IsExpanded = isExpanded)))
			.ConfigureAwait(false);

		_logger.LogDebug($"{folders.Length} folders are {(isExpanded ? "expanded" : "collapsed")}");
	}

	/// <summary>
	/// Handles changing object hotkeys.
	/// </summary>
	public async Task HandleChangeHotkeysAsync(HotkeysEditorViewModel viewModel, FileModelDto dto)
	{
		if (viewModel.IsSaved)
		{
			await OverwriteFileHotkeysAsync(dto, [.. viewModel.Buffer]).ConfigureAwait(false);
		}

		viewModel.Dispose();

		if (!_settingsManager
			.Settings
			.TrackHotkeys)
		{
			return;
		}

		_ = _keyboardInputHook.StartTrackingAsync(Hierarchy);
	}

	/// <summary>
	/// Handles changing application settings.
	/// </summary>
	public void HandleChangeSettings(bool isSave, AppSettings settings)
	{
		if (isSave)
		{
			_settingsManager.OverwriteSettings(settings);

			_settingsManager.SaveSettingsInFile();
		}
		else
		{
			_settingsManager.ApplyMeterialTheme();
		}

		if (_keyboardInputHook.IsRunning)
		{
			_keyboardInputHook.StopTracking();
		}

		if (!settings.TrackHotkeys)
		{
			return;
		}

		_ = _keyboardInputHook.StartTrackingAsync(Hierarchy);
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

		if (windowSettings.X > 0 && windowSettings.Y > 0)
		{
			window.Position = new(windowSettings.X, windowSettings.Y);
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

		CopyHistorySettings.CopyHistory = copyHistorySettings.CopyHistory;

		CopyHistorySettings.SelectedCopyHistoryItemId = copyHistorySettings.SelectedCopyHistoryItemId;

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

		if (temp.Length > 0 && Hierarchy.FindFileBy(x => x.Hotkeys.SequenceEqual(temp, comparer)) is { } existed)
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

			if (newHotkeys.Length == 0)
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
	/// Renames <see cref="ExplorerModelBase" /> in the database and in the <see cref="TreeView" />.
	/// </summary>
	public async Task<bool> RenameAsync(
		ExplorerModelBaseDto dto,
		string newName,
		DateTime updatedDate,
		CancellationToken token = default)
	{
		if (newName.Equals(dto.Name, StringComparison.Ordinal))
		{
			string warningText = $@"{Strings.IdenticalNames} ""{newName}""";

			ShowWarningSnackbar(warningText);

			_logger.LogWarning(warningText);

			return false;
		}

		PropertyNameValuePair[] properties =
		[
			new PropertyNameValuePair(nameof(ExplorerModelBaseDto.Name), newName),
			new PropertyNameValuePair(nameof(ExplorerModelBaseDto.UpdatedDate), updatedDate)
		];

		if (!await _dbAccess
			.UpdatePropertiesAsync(dto.Id, token, properties)
			.ConfigureAwait(false))
		{
			string errorText = $@"{Strings.FailedToRename} ""{dto.Name}"" {Strings.To} ""{newName}""";

			ShowErrorSnackbar(errorText);

			_logger.LogError(errorText);

			return false;
		}

		string successText = $@"""{dto.Name}"" {Strings.RenamedTo} ""{newName}""";

		ShowInfoSnackbar(successText);

		_logger.LogInformation(successText);

		dto.Name = newName;

		dto.UpdatedDate = updatedDate;

		return true;
	}

	/// <inheritdoc />
	public override void SaveCopyHistory()
	{
		if (RightSideSheetContent is not CopyHistoryView view)
		{
			return;
		}

		SaveCopyHistory(view.ViewModel);
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
		Window? window,
		Guid id,
		CancellationToken token = default)
	{
		if (window is null || Hierarchy.FindById(id) is not { } found)
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
			treeView = window.FindVisualChild<TreeView>();

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

		if (treeView.FindVisualChild<ScrollViewer>() is { } scrollViewer)
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
	#endregion

	#region Service
	/// <summary>
	/// Returns <c>True</c> if file can be edited or executed.
	/// </summary>
	private static bool CanBeEditedOrExecuted(FileModelDto? dto)
	{
		return dto is not null
			&& dto.EncryptionStatus != EncryptionStatus.Encrypted
			&& !dto.IsEdited
			&& !dto.IsExecuted;
	}

	/// <summary>
	/// Validates <see cref="HideFileContentsCommand" />.
	/// </summary>
	private static bool CanExecuteFileContents(FileModelDto? dto)
	{
		return dto is not null && dto.EncryptionStatus == EncryptionStatus.Decrypted;
	}

	/// <summary>
	/// Validates <see cref="HideFolderContentsCommand" />.
	/// </summary>
	private static bool CanExecuteHideFolderContents(FolderModelDto? dto)
	{
		return dto is not null
			&& dto.EncryptionStatus.IsNotDefault()
			&& dto.AnyChild(x => x.EncryptionStatus == EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// Validates <see cref="ShowFileContentsCommand" />.
	/// </summary>
	private static bool CanExecuteShowFileContents(FileModelDto? dto)
	{
		return dto is not null && dto.EncryptionStatus == EncryptionStatus.Encrypted;
	}

	/// <summary>
	/// Validates <see cref="ShowFileContentsCommand" />.
	/// </summary>
	private static bool CanExecuteShowFolderContents(FolderModelDto? dto)
	{
		return dto is not null
			&& dto.EncryptionStatus.IsNotDefault()
			&& dto.AnyChild(x => x.EncryptionStatus == EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// Returns a reference to the collection to add the object to.
	/// </summary>
	private static Collection<ExplorerModelBaseDto> GetCollectionToAdd(
		FolderModelDto? parent,
		Collection<ExplorerModelBaseDto> collection) => parent switch
		{
			not null => parent.Children,
			null => collection
		};

	/// <summary>
	/// Returns a reference to the collection containing the object to be removed.
	/// </summary>
	private static Collection<ExplorerModelBaseDto> GetCollectionToDelete(
		ExplorerModelBaseDto target,
		Collection<ExplorerModelBaseDto> collection) => target.Parent switch
		{
			not null => target.Parent.Children,
			null => collection
		};

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

	/// <summary>
	/// Validates <see cref="ChangePasswordCommand" />.
	/// </summary>
	private bool CanExecuteChangePassword(FolderModelDto? dto) => IsNotReadOnly() && dto?.IsPasswordKeeper() == true;

	/// <summary>
	/// Validates <see cref="CloseAllExecutedFilesCommand" />.
	/// </summary>
	private bool CanExecuteCloseAllExecutedFiles() => ExecutedFiles.Count > 0;

	/// <summary>
	/// Validates <see cref="DecryptFolderCommand" />.
	/// </summary>
	private bool CanExecuteDecryptFolder(FolderModelDto? dto)
	{
		return IsNotReadOnly()
			&& dto?.IsPasswordKeeper() == true
			&& dto.EncryptionStatus == EncryptionStatus.Encrypted
			&& dto.Children.AllBy(x => x.EncryptionStatus == EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// Validates <see cref="DeleteCommand" />.
	/// </summary>
	private bool CanExecuteDelete(ExplorerModelBaseDto? dto)
	{
		return IsNotReadOnly() && (dto is not null || IsSelectedObjectNotNull());
	}

	/// <summary>
	/// Validates <see cref="EncryptFolderCommand" />.
	/// </summary>
	private bool CanExecuteEncryptFolder(FolderModelDto? dto)
	{
		return IsNotReadOnly()
			&& dto?.IsPasswordKeeper() == false
			&& !dto.AnyParent(x => x.IsPasswordKeeper())
			&& !dto.AnyChild(x => x.EncryptionStatus != EncryptionStatus.None);
	}

	/// <summary>
	/// Validates <see cref="ExportCommand" />.
	/// </summary>
	private bool CanExecuteExport() => Hierarchy.Count > 0;

	/// <summary>
	/// Validates <see cref="HideAllFilesCommand" />.
	/// </summary>
	private bool CanExecuteHideAllFiles() => Hierarchy.ContainsBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted);

	/// <summary>
	/// Validates <see cref="RenameCommand" />.
	/// </summary>
	private bool CanExecuteRename(ExplorerModelBaseDto? dto)
	{
		return IsNotReadOnly() && (dto is not null || IsSelectedObjectNotNull());
	}

	/// <summary>
	/// Validates <see cref="ShowFavoritesCommand" />.
	/// </summary>
	private bool CanExecuteShowFavorites() => Hierarchy.ContainsBy(x => x.IsFavorite);

	/// <summary>
	/// Closes file that is being edited or executed;
	/// </summary>
	private void CloseFile(FileModelDto file)
	{
		if (file.IsEdited)
		{
			EditFiles
				.ViewModel
				.CloseTab(file);
		}

		if (file.IsExecuted)
		{
			CloseExecutedFile(file);
		}
	}

	/// <summary>
	/// Counts the number of objects in <see cref="Hierarchy" />.
	/// </summary>
	private void CountHierarchy() => BottomLeftCornerInfo = Hierarchy.GetCount().AsString();

	/// <summary>
	/// Returns <c>True</c> if <see cref="IsReadOnly" /> is <c>False</c>.
	/// </summary>
	private bool IsNotReadOnly() => !IsReadOnly;

	/// <summary>
	/// Returns <c>True</c> if <see cref="SelectedObject" /> is not null.
	/// </summary>
	private bool IsSelectedObjectNotNull() => SelectedObject is not null;

	/// <summary>
	/// Tries to close edited or executed files if any.
	/// </summary>
	private async Task<bool> TryCloseEditedExecutedFilesAsync(
		FileModelDto[] files,
		CancellationToken token = default)
	{
		if (files.Any(x => x.IsEdited || x.IsExecuted))
		{
			if (!await _dialogService
				.RequestUserCloseFilesAsync(token)
				.ConfigureAwait(true))
			{
				return false;
			}

			CloseFiles(
				files.Where(x => x.IsEdited),
				files.Where(x => x.IsExecuted));
		}

		return true;
	}

	/// <summary>
	/// Updates the <see cref="FileModelDto.IsFavorite" /> property of related object in the database.
	/// </summary>
	private Task<bool> UpdateFileIsFavoriteInDatabaseAsync(
		FileModelDto dto,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] in int lineNumber = 0,
		CancellationToken token = default)
	{
		const string propertyName = nameof(FileModelDto.IsFavorite);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerModelBaseDto.EntityType),
			nameof(ExplorerModelBaseDto.Name),
			propertyName)}", filePath, callerName, lineNumber);

		return _dbAccess.UpdatePropertyAsync(
			dto.Id,
			propertyName,
			dto.IsFavorite,
			token);
	}

	/// <summary>
	/// Updates the <see cref="FolderModelDto.IsExpanded" /> property of related object in the database.
	/// </summary>
	private Task<bool> UpdateFolderIsExpandedInDatabaseAsync(
		FolderModelDto dto,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] in int lineNumber = 0,
		CancellationToken token = default)
	{
		const string propertyName = nameof(FolderModelDto.IsExpanded);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerModelBaseDto.EntityType),
			nameof(ExplorerModelBaseDto.Name),
			propertyName)}", filePath, callerName, lineNumber);

		return _dbAccess.UpdatePropertyAsync(
			dto.Id,
			propertyName,
			dto.IsExpanded,
			token);
	}

	/// <summary>
	/// Updates the <see cref="ExplorerModelBaseDto.IsSelected" /> property of related object in the database.
	/// </summary>
	private Task<bool> UpdateIsSelectedInDatabaseAsync(
		ExplorerModelBaseDto dto,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] in int lineNumber = 0,
		CancellationToken token = default)
	{
		const string propertyName = nameof(ExplorerModelBaseDto.IsSelected);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerModelBaseDto.EntityType),
			nameof(ExplorerModelBaseDto.Name),
			propertyName)}", filePath, callerName, lineNumber);

		return _dbAccess.UpdatePropertyAsync(
			dto.Id,
			propertyName,
			dto.IsSelected,
			token);
	}
	#endregion
}
