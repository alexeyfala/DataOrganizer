using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Dataset;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Enums;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Dataset;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Models.Dataset;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>DatasetEditorView</c>.
/// </summary>
public sealed partial class DatasetEditorViewModel : EmbeddedEditorViewModelBase
{
	#region Properties
	/// <summary>
	/// The records of the dataset being edited.
	/// </summary>
	public ObservableCollection<DatasetRecordBase> Records { get; } = [];

	/// <summary>
	/// The record at the top of the list and how far the list is scrolled into it.
	/// </summary>
	[ObservableProperty]
	public partial DatasetEditorState? ViewState { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Reads the records and the stored view state once.
	/// </summary>
	[RelayCommand]
	internal async Task ContainerLoaded()
	{
		if (IsInitialized)
		{
			return;
		}

		await LoadAsync().ConfigureAwait(true);

		_logger.LogInformation($@"Content is initialized in ""{GetType().Name}""");
	}

	/// <summary>
	/// Handles when the user has manually changed <see cref="ValueRecordBase.IsHidden" />.
	/// </summary>
	[RelayCommand]
	internal Task IsHiddenChanged()
	{
		// Do not check "IsReadOnly" in "CanExecute", otherwise "ToggleButton" will not be enabled.
		if (IsReadOnly)
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation($"{nameof(ValueRecordBase.IsHidden)} property of record is changed");

		return SaveContentsAsync(FileContentsChange.View);
	}

	/// <summary>
	/// Moves a dragged record to the place it is dropped at, highlights it and saves the records.
	/// </summary>
	[RelayCommand]
	internal Task MoveRecord(DatasetRecordMove? move)
	{
		// The drop target runs the command without asking "CanExecute", so the rule is checked here.
		if (move is null || !IsEditingEnabled)
		{
			return Task.CompletedTask;
		}

		if (!DatasetRecordTree.Move(Records, move.Record, move.Target, move.Index))
		{
			return Task.CompletedTask;
		}

		_dispatcher.Post(() => move.Record.PulseHighlight(), UiPriority.Background);

		_logger.LogInformation("Records reordered by drag-and-drop");

		return SaveContentsAsync(FileContentsChange.Edit);
	}

	/// <summary>
	/// Adds a <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private async Task AddGroup(RecordsGroup? group)
	{
		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.AddGroup,
			IsSensitive = IsEncrypted,
			KeyHint = Strings.Name
		};

		if (await _textInputPrompt
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } result)
		{
			return;
		}

		await AddGroupAsync(result.Key, group).ConfigureAwait(false);
	}

	/// <summary>
	/// Adds a <see cref="KeyValueRecord" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private async Task AddKeyValue(RecordsGroup? group)
	{
		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.AddKeyAndValue,
			IsSensitive = IsEncrypted,
			KeyHint = Strings.Key,
			MaskValueInput = IsEncrypted,
			ValueHint = Strings.Value
		};

		if (await _textInputPrompt
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } result)
		{
			return;
		}

		await AddKeyValueAsync(
			result.Key,
			result.Value,
			group).ConfigureAwait(false);
	}

	/// <summary>
	/// Adds a <see cref="ValueRecord" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private async Task AddValue(RecordsGroup? group)
	{
		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.AddValue,
			IsSensitive = IsEncrypted,
			KeyHint = Strings.Name,
			MaskKeyInput = IsEncrypted
		};

		if (await _textInputPrompt
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } result)
		{
			return;
		}

		await AddValueAsync(result.Key, group).ConfigureAwait(false);
	}

	/// <summary>
	/// Collapses all <see cref="RecordsGroup" /> in <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasGroups))]
	private Task Collapse(RecordsGroup? group) => ExpandCollapseAsync(group, false);

	/// <summary>
	/// Copies <see cref="KeyValueRecord" /> key and value to system clipboard.
	/// </summary>
	[RelayCommand]
	private async Task CopyKeyValueToClipboard(KeyValueRecord? record)
	{
		if (record is null)
		{
			return;
		}

		string text = $"{record.Key}    {record.Value}";

		if (!await _clipboardWriter
			.WriteTextAsync(text, isSensitive: IsEncrypted)
			.ConfigureAwait(true))
		{
			return;
		}

		record.PulseHighlight();
	}

	/// <summary>
	/// Deletes a <see cref="RecordsGroup" /> from <see cref="Records" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private Task DeleteGroup(RecordsGroup? group)
	{
		if (group is null)
		{
			return Task.CompletedTask;
		}

		return DeleteRecordAsync(group, group.Name);
	}

	/// <summary>
	/// Deletes a <see cref="KeyValueRecord" /> from <see cref="Records" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private Task DeleteKeyValueRecord(KeyValueRecord? record)
	{
		if (record is null)
		{
			return Task.CompletedTask;
		}

		return DeleteRecordAsync(record, record.Key);
	}

	/// <summary>
	/// Deletes a <see cref="ValueRecord" /> from <see cref="Records" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private Task DeleteValueRecord(ValueRecord? record)
	{
		if (record is null)
		{
			return Task.CompletedTask;
		}

		return DeleteRecordAsync(record, record.Value);
	}

	/// <summary>
	/// Edits a <see cref="KeyValueRecord" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private async Task EditKeyValue(KeyValueRecord? record)
	{
		if (record is null)
		{
			return;
		}

		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.Save,
			IsSensitive = IsEncrypted,
			Key = record.Key,
			KeyHint = Strings.Key,
			MaskValueInput = record.IsHidden,
			Value = record.Value,
			ValueHint = Strings.Value
		};

		if (await _textInputPrompt
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } result)
		{
			return;
		}

		await EditKeyValueAsync(
			record,
			result.Key,
			result.Value).ConfigureAwait(false);
	}

	/// <summary>
	/// Edits <see cref="DatasetRecordBase.Note" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private async Task EditNote(DatasetRecordBase? record)
	{
		if (record is null)
		{
			return;
		}

		// The value of a record never becomes a header: it is masked when IsHidden is set.
		string? header = record switch
		{
			RecordsGroup group => group.Name,
			KeyValueRecord keyValue => keyValue.Key,
			_ => null
		};

		TextInputResult result = await _textInputPrompt.RequestMultilineTextAsync(
			record.Note,
			header,
			isSensitive: IsEncrypted)
			.ConfigureAwait(false);

		if (!result.IsConfirmed)
		{
			return;
		}

		await EditNoteAsync(record, result.Value).ConfigureAwait(false);
	}

	/// <summary>
	/// Edits a <see cref="ValueRecord" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private async Task EditValue(ValueRecord? record)
	{
		if (record is null)
		{
			return;
		}

		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.Save,
			IsSensitive = IsEncrypted,
			Key = record.Value,
			KeyHint = Strings.Edit,
			MaskKeyInput = record.IsHidden
		};

		if (await _textInputPrompt
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } result)
		{
			return;
		}

		await EditValueAsync(record, result.Key).ConfigureAwait(false);
	}

	/// <summary>
	/// Expands all <see cref="RecordsGroup" /> in <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasGroups))]
	private Task Expand(RecordsGroup? group) => ExpandCollapseAsync(group, true);

	/// <summary>
	/// Expands or collapses all <see cref="RecordsGroup" /> in <see cref="Records" /> depending on <paramref name="expand"/> value.
	/// </summary>
	[RelayCommand]
	private Task ExpandCollapse(bool expand) => ExpandCollapseAsync(null, expand);

	/// <summary>
	/// Saves the records as a change of the view once the user has expanded or collapsed a group.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private Task GroupExpandedCollapsedByUser() => SaveContentsAsync(FileContentsChange.View);

	/// <summary>
	/// Sets <see cref="ValueRecordBase.IsHidden" /> value to <c>True</c> to all records in <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private Task Hide(RecordsGroup? group) => ShowHideAsync(group, true);

	/// <summary>
	/// Renames a <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsEditingEnabled))]
	private async Task RenameGroup(RecordsGroup? group)
	{
		if (group is null)
		{
			return;
		}

		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.Save,
			Key = group.Name,
			KeyHint = Strings.Rename
		};

		if (await _textInputPrompt
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } result)
		{
			return;
		}

		await RenameGroupAsync(group, result.Key).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets <see cref="ValueRecordBase.IsHidden" /> value to <c>False</c> to all records in <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private Task Show(RecordsGroup? group) => ShowHideAsync(group, false);

	/// <summary>
	/// Sets <see cref="ValueRecordBase.IsHidden" /> value from <paramref name="hide"/> to all records in <see cref="Records" />.
	/// </summary>
	[RelayCommand]
	private Task ShowHide(bool hide) => ShowHideAsync(null, hide);

	/// <summary>
	/// Sorts <see cref="RecordsGroup" /> child objects in <see cref="ListSortDirection.Ascending" /> order.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private async Task SortAscending(RecordsGroup? group)
	{
		if (!await _confirmationPrompt
			.RequestYesNoAsync(Strings.SortAscending + "?")
			.ConfigureAwait(false))
		{
			return;
		}

		await SortAsync(group, ListSortDirection.Ascending).ConfigureAwait(false);
	}

	/// <summary>
	/// Sorts <see cref="Records" /> ascending or descending order.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasRecords))]
	private async Task SortAscendingDescending(ListSortDirection direction)
	{
		string text = string.Concat(direction switch
		{
			ListSortDirection.Ascending => Strings.SortAscending,
			ListSortDirection.Descending => Strings.SortDescending,
			_ => throw new NotImplementedException()
		}, "?");

		if (!await _confirmationPrompt
			.RequestYesNoAsync(text)
			.ConfigureAwait(false))
		{
			return;
		}

		await SortAsync(null, direction).ConfigureAwait(false);
	}

	/// <summary>
	/// Sorts <see cref="RecordsGroup" /> child objects in <see cref="ListSortDirection.Descending" /> order.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private async Task SortDescending(RecordsGroup? group)
	{
		if (!await _confirmationPrompt
			.RequestYesNoAsync(Strings.SortDescending + "?")
			.ConfigureAwait(false))
		{
			return;
		}

		await SortAsync(group, ListSortDirection.Descending).ConfigureAwait(false);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IClipboardTextWriter" />
	private readonly IClipboardTextWriter _clipboardWriter;

	/// <inheritdoc cref="IConfirmationPrompt" />
	private readonly IConfirmationPrompt _confirmationPrompt;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ITextInputPrompt" />
	private readonly ITextInputPrompt _textInputPrompt;

	/// <summary>
	/// Change of the records whose save failed; <c>null</c> while every change is saved.
	/// </summary>
	private FileContentsChange? _unsavedChange;
	#endregion

	#region Constructors
	public DatasetEditorViewModel(
		IClipboardTextWriter clipboardWriter,
		IConfirmationPrompt confirmationPrompt,
		IDbFailureReporter dbFailureReporter,
		IDispatcherAccessor dispatcher,
		IEditorStateStore editorStateStore,
		IFileContentsReader fileContentsReader,
		IFileContentsWriter fileContentsWriter,
		IJsonSerializer jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ISessionReadOnlyMode sessionReadOnlyMode,
		ITaskExceptionHandler exceptionHandler,
		ITextInputPrompt textInputPrompt) : base(
			dbFailureReporter,
			editorStateStore,
			fileContentsReader,
			fileContentsWriter,
			jsonSerializer,
			logger,
			messenger,
			notification,
			sessionReadOnlyMode,
			exceptionHandler)
	{
		_clipboardWriter = clipboardWriter;

		_confirmationPrompt = confirmationPrompt;

		_dispatcher = dispatcher;

		_textInputPrompt = textInputPrompt;
	}
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="ViewState" /> changes.
	/// </summary>
	partial void OnViewStateChanged(DatasetEditorState? value)
	{
		// The state read with the records is not a change, and the view may still report its state for a moment after
		// the file has been closed.
		if (value is not { } state || !CanSaveEditorState)
		{
			return;
		}

		SaveEditorState(state);
	}
	#endregion

	#region Methods
	/// <inheritdoc cref="AddGroup" />
	internal Task AddGroupAsync(
		string name,
		RecordsGroup? group,
		CancellationToken token = default)
	{
		_logger.LogInformation("Adding a records group");

		RecordsGroup record = new()
		{
			Name = name
		};

		DatasetRecordTree.Add(Records, record, group);

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <inheritdoc cref="AddKeyValue" />
	internal Task AddKeyValueAsync(
		string key,
		string? value,
		RecordsGroup? group,
		CancellationToken token = default)
	{
		_logger.LogInformation("Adding a key & value record");

		KeyValueRecord record = new()
		{
			IsHidden = IsEncrypted,
			Key = key,
			Value = value
		};

		DatasetRecordTree.Add(Records, record, group);

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <inheritdoc cref="AddValue" />
	internal Task AddValueAsync(
		string value,
		RecordsGroup? group,
		CancellationToken token = default)
	{
		_logger.LogInformation("Adding a value record");

		ValueRecord record = new()
		{
			IsHidden = IsEncrypted,
			Value = value
		};

		DatasetRecordTree.Add(Records, record, group);

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <summary>
	/// Deletes a record from <see cref="Records" />.
	/// </summary>
	internal Task DeleteRecordAsync(DatasetRecordBase record, CancellationToken token = default)
	{
		_logger.LogInformation("Deleting a record");

		DatasetRecordTree.Delete(Records, record);

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <inheritdoc cref="EditKeyValue" />
	internal Task EditKeyValueAsync(
		KeyValueRecord target,
		string key,
		string? value,
		CancellationToken token = default)
	{
		if (string.Equals(key, target.Key) && string.Equals(value, target.Value))
		{
			_logger.LogInformation("Ignoring edit of key and value record because key and value are the same");

			return Task.CompletedTask;
		}

		_logger.LogInformation("Editing a key and value record");

		target.Key = key;

		target.Value = value;

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <inheritdoc cref="EditNote(DatasetRecordBase?)" />
	internal Task EditNoteAsync(
		DatasetRecordBase record,
		string? note,
		CancellationToken token = default)
	{
		_logger.LogInformation("Editing a note of a record");

		record.Note = note;

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <inheritdoc cref="EditValue(ValueRecord?)" />
	internal Task EditValueAsync(
		ValueRecord target,
		string value,
		CancellationToken token = default)
	{
		if (string.Equals(value, target.Value))
		{
			_logger.LogInformation("Ignoring edit of value record because value is the same");

			return Task.CompletedTask;
		}

		_logger.LogInformation("Editing a value record");

		target.Value = value;

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <summary>
	/// Expands or collapses all <see cref="RecordsGroup" /> in <see cref="RecordsGroup.Children" /> or in <see cref="Records" /> depending on <paramref name="expand"/> value.
	/// </summary>
	internal async Task ExpandCollapseAsync(
		RecordsGroup? group,
		bool expand,
		CancellationToken token = default)
	{
		IEnumerable<DatasetRecordBase> source = group is not null ? [group] : Records;

		RecordsGroup[] groups = [.. DatasetRecordTree
			.Flatten(source)
			.OfType<RecordsGroup>()
			.Where(x => x.IsExpanded != expand)];

		if (groups.IsEmpty())
		{
			return;
		}

		_logger.LogInformation($"{(expand ? "Expand" : "Collapse")} all groups");

		await groups
			.ForEachAsync(x => _dispatcher.PostAsync(() => x.IsExpanded = expand, UiPriority.Background))
			.ConfigureAwait(false);

		await SaveContentsAsync(FileContentsChange.View, token).ConfigureAwait(false);
	}

	/// <inheritdoc cref="RenameGroup" />
	internal Task RenameGroupAsync(
		RecordsGroup group,
		string name,
		CancellationToken token = default)
	{
		if (string.Equals(name, group.Name))
		{
			_logger.LogInformation("Ignoring renaming of group name because name is the same");

			return Task.CompletedTask;
		}

		group.Name = name;

		_logger.LogInformation("Renaming a group");

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <summary>
	/// Sets <see cref="ValueRecordBase.IsHidden" /> value from <paramref name="hide"/> to all records in <see cref="RecordsGroup" /> or in <see cref="Records" />.
	/// </summary>
	internal Task ShowHideAsync(
		RecordsGroup? group,
		bool hide,
		CancellationToken token = default)
	{
		ValueRecordBase[] records = [.. DatasetRecordTree
			.Flatten(group is not null ? group.Children : Records)
			.OfType<ValueRecordBase>()
			.Where(x => x.IsHidden != hide)];

		if (records.IsEmpty())
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation($"{(hide ? "Hide" : "Show")} all records");

		records.ForEach(x => x.IsHidden = hide);

		return SaveContentsAsync(FileContentsChange.View, token);
	}

	/// <summary>
	/// Sorts <see cref="RecordsGroup" /> or <see cref="Records" /> in a required order.
	/// </summary>
	internal Task SortAsync(
		RecordsGroup? group,
		ListSortDirection direction,
		CancellationToken token = default)
	{
		_logger.LogInformation("Sort records");

		ObservableCollection<DatasetRecordBase> records = group is not null
			? group.Children
			: Records;

		if (!DatasetRecordTree.Sort(records, direction))
		{
			return Task.CompletedTask;
		}

		return SaveContentsAsync(FileContentsChange.Edit, token);
	}

	/// <inheritdoc />
	protected override Task<bool> FlushAsync(CancellationToken token = default)
	{
		// Each change is saved as it is made, so only a change whose save failed is left to write. Nothing to write is
		// not a failed write, so the checks stay here: a refused save answers "false".
		return IsReadOnly || IsContentUnavailable || _unsavedChange is not { } change
			? Task.FromResult(true)
			: SaveContentsAsync(change, token);
	}

	/// <inheritdoc />
	protected override async Task LoadContentsAsync(byte[] contents)
	{
		if (!contents.IsEmpty())
		{
			try
			{
				Records.AddRange(_jsonSerializer
					.Deserialize<DatasetRecordBase[]>(contents)
					.AsNotNull());
			}
			finally
			{
				contents.ZeroMemory();
			}
		}

		// A dataset that starts without records has no position to restore.
		if (Records.Count > 0)
		{
			// The state reaches the view through a binding, so it is set on the UI thread.
			ViewState = await ReadEditorStateAsync<DatasetEditorState>().ConfigureAwait(true);
		}
	}

	/// <inheritdoc />
	protected override void LogUnopenedContents(FileContentsStatus status, string message)
	{
		if (status != FileContentsStatus.Damaged)
		{
			return;
		}

		_logger.LogError(message);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when <see cref="RecordsGroup" /> has child objects.
	/// </summary>
	private static bool HasChildren(RecordsGroup? group) => group?.Children.Any() ?? false;

	/// <summary>
	/// <c>True</c> when <see cref="RecordsGroup" /> has child <see cref="RecordsGroup" />.
	/// </summary>
	private static bool HasGroups(RecordsGroup? group)
	{
		return group?
			.Children
			.OfType<RecordsGroup>()
			.Any() ?? false;
	}

	/// <inheritdoc cref="DeleteRecordAsync(DatasetRecordBase, CancellationToken)" />
	private async Task DeleteRecordAsync(
		DatasetRecordBase record,
		string? questionText,
		CancellationToken token = default)
	{
		if (!await _confirmationPrompt
			.RequestYesNoAsync($@"{Strings.Delete} ""{questionText}""?", token: token)
			.ConfigureAwait(false))
		{
			return;
		}

		await DeleteRecordAsync(record, token).ConfigureAwait(false);
	}

	/// <summary>
	/// <c>True</c> when <see cref="Records" /> has elements.
	/// </summary>
	private bool HasRecords() => Records.Any();

	/// <summary>
	/// Saves the records, which also hold an earlier change whose save failed; <c>false</c> when they are not saved.
	/// </summary>
	private async Task<bool> SaveContentsAsync(FileContentsChange change, CancellationToken token = default)
	{
		if (IsReadOnly || IsContentUnavailable)
		{
			return false;
		}

		// An edit whose save failed goes with these records, so they are written as an edit.
		if (_unsavedChange == FileContentsChange.Edit)
		{
			change = FileContentsChange.Edit;
		}

		byte[] contents = _jsonSerializer.SerializeToUtf8Bytes(Records);

		try
		{
			FileContentsWriteStatus status = await SaveContentsAsync(contents, change, token).ConfigureAwait(false);

			if (status == FileContentsWriteStatus.Closed)
			{
				_logger.LogError($@"{Strings.FailedToProcessContents} of file ""{FileId}""");
			}

			_unsavedChange = status == FileContentsWriteStatus.Written ? null : change;

			return status == FileContentsWriteStatus.Written;
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, Strings.FailedToSaveFileContents);

			_unsavedChange = change;

			return false;
		}
		finally
		{
			contents.ZeroMemory();
		}
	}
	#endregion
}
