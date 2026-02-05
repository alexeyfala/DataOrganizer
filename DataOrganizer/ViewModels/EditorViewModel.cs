using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Comparation;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
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
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using SharpHook;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

	/// <summary>
	/// Executed in operating system files.
	/// </summary>
	public ObservableCollection<FileModelDto> ExecutedFiles { get; } = [];
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
		if (oldValue is not null && Hierarchy.ConatainsId(oldValue.Id))
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

	/// <inheritdoc cref="EditFilesViewModel.AddTab(FileModelDto)" />
	[RelayCommand(CanExecute = nameof(IsNotOpenedNotExecuted))]
	public void EditFile(FileModelDto? dto)
	{
		if (dto is null)
		{
			return;
		}

		EditFiles
			.ViewModel
			.AddTab(dto);
	}

	/// <summary>
	/// Executes the file in the operating system.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotOpenedNotExecuted))]
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

		if (result.IsDefault() || !result.IsValid)
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

		if (!await _executionEngine
			.ExecuteAsync(dto, result.Contents, IsReadOnly)
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

		_viewLauncher
			.ConfigureFavoritesWindow(Hierarchy)
			.Show();
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
	private Task<object?> Add(FolderModelDto? parent)
	{
		_logger.LogInformation("Adding an object using a dialog");

		EntityCreationView view = _viewFactory.CreateUserControl<EntityCreationView>();

		view
			.ViewModel
			.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			EntityType entityType = view.ViewModel switch
			{
				{ IsFolderSelected: true } => EntityType.Folder,
				{ IsFileSelected: true } => EntityType.File,
				{ IsDatasetSelected: true } => EntityType.DataSet,
				_ => throw new NotImplementedException()
			};

			return AddAsync(view.ViewModel.Name, entityType, parent);
		};

		return DialogHost.Show(view, Dialog_Closing);

		void Dialog_Closing(object sender, DialogClosingEventArgs e)
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
	/// Decrypts file contents in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteDecryptFiles))]
	private Task DecryptFiles(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		return TakeCryptPasswordAsync(dto, CryptoAction.Decrypt);
	}

	/// <summary>
	/// Displays the delete object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteDelete))]
	private Task Delete(ExplorerModelBaseDto? dto)
	{
		ExplorerModelBaseDto? toBeDeleted = dto ?? SelectedObject;

		if (toBeDeleted is null)
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation("Deleting an object using a dialog");

		YesNoQuestionBox view = _viewLauncher.ConfigureYesNoQuestionBox($@"{Strings.Delete} ""{toBeDeleted.Name}""?");

		view
			.ViewModel
			.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			return DeleteAsync(toBeDeleted);
		};

		return DialogHost.Show(view);
	}

	/// <inheritdoc cref="EncryptDecryptAsync" />
	[RelayCommand(CanExecute = nameof(CanExecuteEncryptFiles))]
	private Task EncryptFiles(FolderModelDto? dto)
	{
		if (dto is null)
		{
			return Task.CompletedTask;
		}

		return TakeCryptPasswordAsync(dto, CryptoAction.Encrypt);
	}

	/// <summary>
	/// Expands all folders in <see cref="Hierarchy" />.
	/// </summary>
	[RelayCommand]
	private Task ExpandAllFolders() => ExpandCollapseAllFoldersAsync(true);

	/// <summary>
	/// <see cref="InputElement.DoubleTapped" /> event handler on <see cref="FileModelDto" />.<see cref="DataTemplate" />.
	/// </summary>
	[RelayCommand]
	private void FileDoubleTapped(Control? container)
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

		_logger.LogDebug($"Double tapped on file:{SelectedObject.GetPropertyValues(
			true,
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name))}");
	}

	/// <summary>
	/// Hides file contents in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteHideFileContents))]
	private void HideFileContents(FolderModelDto? dto)
	{
		// TODO: Implement
	}

	/// <summary>
	/// Displays the rename object dialog box.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteRename))]
	private Task Rename(ExplorerModelBaseDto? dto)
	{
		ExplorerModelBaseDto? toBeRenamed = dto ?? SelectedObject;

		if (toBeRenamed is null)
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation("Renaming an object using dialog");

		KeyValueInputView view = _viewLauncher.ConfigureKeyValueInputView(
			defaultButtonText: Strings.Rename,
			key: toBeRenamed.Name,
			keyHint: Strings.Name);

		view
			.ViewModel
			.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			if (view.ViewModel.Key is not { } newName)
			{
				return Task.CompletedTask;
			}

			return RenameAsync(toBeRenamed, newName, DateTime.Now);
		};

		return DialogHost.Show(view);
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

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteShowFileContents))]
	private void ShowFileContents(FolderModelDto? dto)
	{
		// TODO: Implement
	}

	/// <inheritdoc cref="ViewModelBase.ShowInEditorAsync" />
	[RelayCommand]
	private void ShowInList(Guid id) => _ = ShowInEditorAsync(_app.FindWindow<EditorWindow>(), id);
	#endregion

	#region Data
	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IExecutionEngine" />
	private readonly IExecutionEngine _executionEngine;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <summary>
	/// Mapper.
	/// </summary>
	private readonly IMapper _mapper;

	/// <inheritdoc cref="IProcessUtils" />
	private readonly IProcessUtils _processUtils;
	#endregion

	#region Constructors
	public EditorViewModel(
		Application app,
		IAppSettingsManager settingsManager,
		IDbAccess dbAccess,
		IDispatcher dispatcher,
		IEncryptionService encryption,
		IEventSimulator eventSimulator,
		IExecutionEngine executionEngine,
		IFileSystem fileSystem,
		IKeyboardInputHook keyboardInputHook,
		ILogger logger,
		IMapper mapper,
		IProcessUtils processUtils,
		IViewFactory viewFactory,
		IViewLauncher viewLauncher) : base(app, settingsManager, dbAccess, eventSimulator, keyboardInputHook, logger, dispatcher, viewFactory, viewLauncher)
	{
		_encryption = encryption;

		_executionEngine = executionEngine;

		_fileSystem = fileSystem;

		_mapper = mapper;

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
	private void Folder_IsExpandedChanged(object? _, FolderModelDto e)
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
	/// Encrypts/decrypts files in folder.
	/// </summary>
	public async Task<FilesEncryptionResult> EncryptDecryptAsync(
		FolderModelDto dto,
		FileModelDto[] filesDto,
		string password,
		CryptoAction action,
		CancellationToken token = default)
	{
		try
		{
			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(filesDto.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (contents.Length != filesDto.Length || contents.Any(x => !x.IsValid))
			{
				ShowErrorSnackbar(Strings.FailedToLoadFilesContents);

				return FilesEncryptionResult.FailedToLoadContents;
			}

			ContentsIsValidPair[] result = [.. _encryption.EncryptDecryptContents(
				contents,
				TextHelper.Utf8Encoding.GetBytes(password),
				action)];

			if (result.Length != contents.Length
				|| result.Any(x => !x.IsValid)
				|| result.Any(x => x.Id.IsDefault()))
			{
				ShowErrorSnackbar(Strings.FailedToEncryptFilesContents);

				return FilesEncryptionResult.FailedToEncryptContents;
			}

			if (!_dbAccess.BackupDatabase(out var backupFilePath) || string.IsNullOrEmpty(backupFilePath))
			{
				ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

				return FilesEncryptionResult.UnableToCreateDatabaseBackup;
			}

			DateTime updatedDate = DateTime.Now;

			Dictionary<Guid, PropertyNameValuePair[]> relations = result.ToDictionary(x => x.Id, x =>
			{
				return new PropertyNameValuePair[]
				{
					new(nameof(FileModel.Contents), x.Contents),
					new(nameof(FileModel.UpdatedDate), updatedDate)
				};
			});

			if (!await _dbAccess
				.UpdatePropertiesAsync(relations, token)
				.ConfigureAwait(false))
			{
				await RestoreDatabaseAsync().ConfigureAwait(false);

				DeleteBackupFile();

				return FilesEncryptionResult.FailedToSaveContents;
			}

			string? passwordHash = action switch
			{
				CryptoAction.Encrypt => _encryption.EnhancedHashPassword(password),
				CryptoAction.Decrypt => null,
				_ => throw new NotImplementedException()
			};

			if (!await _dbAccess.UpdatePropertyAsync(
				id: dto.Id,
				propertyName: nameof(FolderModel.PasswordHash),
				value: passwordHash,
				token: token).ConfigureAwait(false))
			{
				await RestoreDatabaseAsync().ConfigureAwait(false);

				DeleteBackupFile();

				return FilesEncryptionResult.FailedToSavePasswordHash;
			}

			ExplorerModelBaseDto[] objects =
			[
				.. dto.ToEnumerable(),
				.. dto.Children.GetFolders(),
				.. filesDto
			];

			EncryptionStatus newStatus = action switch
			{
				CryptoAction.Encrypt => EncryptionStatus.Encrypted,
				CryptoAction.Decrypt => EncryptionStatus.None,
				_ => throw new NotImplementedException()
			};

			objects.ForEach(x => x.EncryptionStatus = newStatus);

			dto.PasswordHash = passwordHash;

			DeleteBackupFile();

			return FilesEncryptionResult.Encrypted;

			async Task RestoreDatabaseAsync()
			{
				ShowErrorSnackbar(
					Strings.FailedToEncryptFilesContents +
					Environment.NewLine +
					Strings.TheDatabaseWillBeRestored);

				await _dbAccess
					.RestoreFromBackupAsync(backupFilePath, token)
					.ConfigureAwait(false);
			}

			void DeleteBackupFile()
			{
				SqliteConnection.ClearAllPools();

				_fileSystem.EraseAndDeleteFile(backupFilePath);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return FilesEncryptionResult.ExceptionThrown;
		}
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
			.GetParents()
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

	/// <summary>
	/// Takes a password for encryption/decryption files in folder.
	/// </summary>
	public Task TakeCryptPasswordAsync(
		FolderModelDto dto,
		CryptoAction action,
		CancellationToken token = default)
	{
		FileModelDto[] filesDto = [.. dto
			.Children
			.GetFiles()];

		if (filesDto.Length == 0)
		{
			ShowInfoSnackbar(action switch
			{
				CryptoAction.Encrypt => Strings.ThereAreNoFilesToEncrypt,
				CryptoAction.Decrypt => Strings.ThereAreNoFilesToDecrypt,
				_ => throw new NotImplementedException()
			});

			return Task.CompletedTask;
		}

		if (filesDto.Any(x => x.IsEdited || x.IsExecuted))
		{
			ShowInfoSnackbar(Strings.YouMustCloseTheFilesYouAreEditing);

			return Task.CompletedTask;
		}

		_logger.LogInformation("Show password box");

		PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return Task.CompletedTask;
		}

		view
			.ViewModel
			.DefaultPressedCallback = async () =>
			{
				DialogOverlayPopupHost? popupHost = view.FindLogicalParent<DialogOverlayPopupHost>();

				try
				{
					DialogHost.Close(null);

					if (view
						.ViewModel
						.Password is not { } password)
					{
						return;
					}

					try
					{
						Func<bool> condition = () => popupHost?.IsActuallyOpen == false;

						await condition
							.WaitAsync(300, 10)
							.ConfigureAwait(false);

						IsActionInProgress = true;

						if (action == CryptoAction.Decrypt
							&& dto.PasswordHash is { } passwordHash
							&& !_encryption.EnhancedVerify(password, passwordHash))
						{
							ShowErrorSnackbar(Strings.IncorrectPassword);

							return;
						}

						await EncryptDecryptAsync(
							dto,
							filesDto,
							password,
							action,
							token).ConfigureAwait(false);
					}
					finally
					{
						IsActionInProgress = false;
					}
				}
				finally
				{
					popupHost = null;
				}
			};

		return DialogHost.Show(view);
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="HideFileContentsCommand" />.
	/// </summary>
	private static bool CanExecuteHideFileContents(FolderModelDto? dto)
	{
		return dto is not null
			&& dto.EncryptionStatus.IsNotDefault()
			&& dto.AnyChild(x => x.EncryptionStatus == EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// Validates <see cref="ShowFileContentsCommand" />.
	/// </summary>
	private static bool CanExecuteShowFileContents(FolderModelDto? dto)
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
	/// Returns <c>True</c> if <see cref="FileModelDto.IsExecuted" /> and <see cref="FileModelDto.IsEdited" /> are <c>False</c>.
	/// </summary>
	private static bool IsNotOpenedNotExecuted(FileModelDto? dto)
	{
		return dto is not null
			&& !dto.IsEdited
			&& !dto.IsExecuted;
	}

	/// <summary>
	/// Validates <see cref="CloseAllExecutedFilesCommand" />.
	/// </summary>
	private bool CanExecuteCloseAllExecutedFiles() => ExecutedFiles.Count > 0;

	/// <summary>
	/// Validates <see cref="DecryptFilesCommand" />.
	/// </summary>
	private bool CanExecuteDecryptFiles(FolderModelDto? dto)
	{
		return IsNotReadOnly()
			&& dto is not null
			&& !string.IsNullOrEmpty(dto.PasswordHash);
	}

	/// <summary>
	/// Validates <see cref="DeleteCommand" />.
	/// </summary>
	private bool CanExecuteDelete() => IsSelectedObjectNotNull() && IsNotReadOnly();

	/// <summary>
	/// Validates <see cref="EncryptFilesCommand" />.
	/// </summary>
	private bool CanExecuteEncryptFiles(FolderModelDto? dto)
	{
		return IsNotReadOnly()
			&& dto is not null
			&& string.IsNullOrEmpty(dto.PasswordHash)
			&& !dto.AnyParent(x => !string.IsNullOrEmpty(x.PasswordHash));
	}

	/// <summary>
	/// Validates <see cref="RenameCommand" />.
	/// </summary>
	private bool CanExecuteRename() => IsSelectedObjectNotNull() && IsNotReadOnly();

	/// <summary>
	/// Validates <see cref="ShowFavoritesCommand" />.
	/// </summary>
	private bool CanExecuteShowFavorites() => Hierarchy.ConatainsBy(x => x.IsFavorite);

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
