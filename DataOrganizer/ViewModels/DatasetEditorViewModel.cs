using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Models;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="DatasetEditorView" />.
/// </summary>
public sealed partial class DatasetEditorViewModel : EditorViewModelBase, IFileEditor
{
	#region Properties
	/// <inheritdoc />
	public Guid FileId { get; set; }

	/// <inheritdoc />
	public string? InitialProperties { get; set; }

	/// <inheritdoc />
	public bool IsInitialized { get; private set; }

	/// <summary>
	/// Records.
	/// </summary>
	public ObservableCollection<DatasetRecordBase> Records { get; } = [];

	/// <inheritdoc />
	public Action<string>? SetPropertiesCallback { get; set; }

	/// <inheritdoc />
	public Action<DateTime>? SetUpdatedDateCallback { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Handles when the user has manually changed <see cref="ValueRecord.IsHidden" />.
	/// </summary>
	[RelayCommand]
	public Task IsHiddenChanged()
	{
		// Do not check "IsReadOnly" in "CanExecute", otherwise "ToggleButton" will not be enabled.
		if (IsReadOnly)
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation($"{nameof(ValueRecord.IsHidden)} property of record is changed");

		return SaveContentsAsync();
	}

	/// <summary>
	/// Adds a <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private Task<object?> AddGroup(RecordsGroup? group)
	{
		KeyValueInputView view = _viewLauncher.ConfigureKeyValueInputView(
			defaultButtonText: Strings.AddGroup,
			keyHint: Strings.Name);

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			if (view.ViewModel.Key is not { } name)
			{
				return Task.CompletedTask;
			}

			return AddGroupAsync(name, group);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Adds a <see cref="KeyValueRecord" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private Task<object?> AddKeyValue(RecordsGroup? group)
	{
		KeyValueInputView view = _viewLauncher.ConfigureKeyValueInputView(
			defaultButtonText: Strings.AddKeyAndValue,
			keyHint: Strings.Key,
			valueHint: Strings.Value);

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			if (view.ViewModel.Key is not { } key)
			{
				return Task.CompletedTask;
			}

			return AddKeyValueAsync(key, view.ViewModel.Value, group);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Adds a <see cref="ValueRecord" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private Task<object?> AddValue(RecordsGroup? group)
	{
		KeyValueInputView view = _viewLauncher.ConfigureKeyValueInputView(
			defaultButtonText: Strings.AddValue,
			keyHint: Strings.Name);

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			if (view.ViewModel.Key is not { } value)
			{
				return Task.CompletedTask;
			}

			return AddValueAsync(value, group);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Collapses all <see cref="RecordsGroup" /> in <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasGroups))]
	private Task Collapse(RecordsGroup? group) => ExpandCollapseAsync(group, true);

	/// <summary>
	/// Handles the <see cref="Control.Loaded" /> event of <see cref="ItemsControl" />.
	/// </summary>
	[RelayCommand]
	private Task ContainerLoaded(ScrollViewer? scrollViewer) => LoadDataAsync(scrollViewer);

	/// <summary>
	/// Copies <see cref="KeyValueRecord" /> key and value to system clipboard.
	/// </summary>
	[RelayCommand]
	private Task CopyKeyValueToClipboard(KeyValueRecord? record)
	{
		if (record is null || _app.FindClipboard() is not { } clipboard)
		{
			return Task.CompletedTask;
		}

		try
		{
			return clipboard.SetTextAsync($"{record.Key}    {record.Value}");
		}
		finally
		{
			record.IsHighlight = true;

			record.IsHighlight = false;
		}
	}

	/// <summary>
	/// Deletes a <see cref="RecordsGroup" /> from <see cref="Records" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private Task EditKeyValue(KeyValueRecord? record)
	{
		if (record is null)
		{
			return Task.CompletedTask;
		}

		KeyValueInputView view = _viewLauncher.ConfigureKeyValueInputView(
			defaultButtonText: Strings.Save,
			key: record.Key,
			keyHint: Strings.Key,
			value: record.Value,
			valueHint: Strings.Value);

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			if (view.ViewModel.Key is not { } key)
			{
				return Task.CompletedTask;
			}

			return EditKeyValueAsync(record, key, view.ViewModel.Value);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Edits <see cref="DatasetRecordBase.Note" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private Task EditNote(DatasetRecordBase? record)
	{
		if (record is null)
		{
			return Task.CompletedTask;
		}

		MultilineTextEditView view = _viewLauncher.ConfigureMultilineTextEditView(record.Note);

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			return EditNoteAsync(record, view.ViewModel.Text);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Edits a <see cref="ValueRecord" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private Task EditValue(ValueRecord? record)
	{
		if (record is null)
		{
			return Task.CompletedTask;
		}

		KeyValueInputView view = _viewLauncher.ConfigureKeyValueInputView(
			defaultButtonText: Strings.Save,
			key: record.Value,
			keyHint: Strings.Edit);

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			if (view.ViewModel.Key is not { } value)
			{
				return Task.CompletedTask;
			}

			return EditValueAsync(record, value);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Expands all <see cref="RecordsGroup" /> in <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasGroups))]
	private Task Expand(RecordsGroup? group) => ExpandCollapseAsync(group, false);

	/// <summary>
	/// Expands or collapses all <see cref="RecordsGroup" /> in <see cref="Records" /> depending on <paramref name="expand"/> value.
	/// </summary>
	[RelayCommand]
	private Task ExpandCollapse(bool expand) => ExpandCollapseAsync(null, expand);

	/// <summary>
	/// Handles the <see cref="Expander.Expanded" />, <see cref="Expander.Collapsed" /> events by user.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private Task GroupExpandedCollapsedByUser(RoutedEventArgs? e)
	{
		if (e?.Source is not Expander expander || !expander.IsPointerOver)
		{
			return Task.CompletedTask;
		}

		// If this event is not handled, it may fire multiple times, especially in highly nested groups.
		e.Handled = true;

		return SaveContentsAsync();
	}

	/// <summary>
	/// Sets <see cref="ValueRecord.IsHidden" /> value to <c>True</c> to all records in <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private Task Hide(RecordsGroup? group) => ShowHideAsync(group, true);

	/// <summary>
	/// Renames a <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnly))]
	private Task RenameGroup(RecordsGroup? group)
	{
		if (group is null)
		{
			return Task.CompletedTask;
		}

		KeyValueInputView view = _viewLauncher.ConfigureKeyValueInputView(
			defaultButtonText: Strings.Save,
			key: group.Name,
			keyHint: Strings.Rename);

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			if (view.ViewModel.Key is not { } name)
			{
				return Task.CompletedTask;
			}

			return RenameGroupAsync(group, name);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Scrolls the list to the end.
	/// </summary>
	[RelayCommand]
	private void ScrollToEnd(ScrollViewer? container)
	{
		_logger.LogInformation("Scroll records to the end");

		container?.ScrollToEnd();
	}

	/// <summary>
	/// Scrolls the list to the top.
	/// </summary>
	[RelayCommand]
	private void ScrollToTop(ScrollViewer? container)
	{
		_logger.LogInformation("Scroll records to the top");

		container?.ScrollToHome();
	}

	/// <summary>
	/// Sets <see cref="ValueRecord.IsHidden" /> value to <c>False</c> to all records in <see cref="RecordsGroup" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private Task Show(RecordsGroup? group) => ShowHideAsync(group, false);

	/// <summary>
	/// Sets <see cref="ValueRecord.IsHidden" /> value from <paramref name="hide"/> to all records in <see cref="Records" />.
	/// </summary>
	[RelayCommand]
	private Task ShowHide(bool hide) => ShowHideAsync(null, hide);

	/// <inheritdoc cref="EditorViewModelBase.ShowInListAsync(Window?, Guid)" />
	[RelayCommand]
	private void ShowInList(Window? window) => _ = ShowInListAsync(window, FileId);

	/// <summary>
	/// Sorts <see cref="RecordsGroup" /> child objects in <see cref="ListSortDirection.Ascending" /> order.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private Task<object?> SortAscending(RecordsGroup? group)
	{
		YesNoQuestionBox view = _viewLauncher.ConfigureYesNoQuestionBox(Strings.SortAscending + "?");

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			return SortAsync(group, ListSortDirection.Ascending);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Sorts <see cref="Records" /> ascending or descending order.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsAnyRecords))]
	private Task<object?> SortAscendingDescending(ListSortDirection direction)
	{
		string text = string.Concat(direction switch
		{
			ListSortDirection.Ascending => Strings.SortAscending,
			ListSortDirection.Descending => Strings.SortDescending,
			_ => throw new NotImplementedException()
		}, "?");

		YesNoQuestionBox view = _viewLauncher.ConfigureYesNoQuestionBox(text);

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			return SortAsync(null, direction);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Sorts <see cref="RecordsGroup" /> child objects in <see cref="ListSortDirection.Descending" /> order.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private Task<object?> SortDescending(RecordsGroup? group)
	{
		YesNoQuestionBox view = _viewLauncher.ConfigureYesNoQuestionBox(Strings.SortDescending + "?");

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			return SortAsync(group, ListSortDirection.Descending);
		};

		return DialogHost.Show(view);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;
	#endregion

	#region Constructors
	public DatasetEditorViewModel(
		App app,
		IDbAccess dbAccess,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		IViewLauncher viewLauncher) : base(app)
	{
		_dbAccess = dbAccess;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_viewLauncher = viewLauncher;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="ScrollViewer.ScrollChanged" /> event handler.
	/// </summary>
	private void ScrollViewer_ScrollChanged(EventPattern<ScrollChangedEventArgs> e)
	{
		lock (_mutex)
		{
			if (e.Sender is not ScrollViewer scrollViewer)
			{
				return;
			}

			DatasetProperties properties = new()
			{
				VerticalScrollOffset = scrollViewer.Offset.Y
			};

			string json = _jsonSerializer.Serialize(properties, AppUtils.JsonOptions);

			SetPropertiesCallback?.Invoke(json);

			if (IsReadOnly)
			{
				return;
			}

			_ = this.SavePropertiesAsync(_dbAccess, _logger, json);
		}
	}
	#endregion

	#region Methods
	/// <summary>
	/// Creates the required number of random <see cref="RecordsGroup" /> objects.
	/// </summary>
	public static IEnumerable<RecordsGroup> CreateGroups(int count)
	{
		string note = TextHelper
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new RecordsGroup()
			{
				Name = AppUtils.CreateRandomString(10),
				Note = note
			};
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="KeyValueRecord" /> objects.
	/// </summary>
	public static IEnumerable<KeyValueRecord> CreateKeyValueRecords(int count)
	{
		string note = TextHelper
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new KeyValueRecord()
			{
				Key = AppUtils.CreateRandomString(10),
				Value = AppUtils.CreateRandomString(10),
				Note = note
			};
		}
	}

	/// <summary>
	/// Creates a random sequence of <see cref="DatasetRecordBase" />.
	/// </summary>
	public static IEnumerable<DatasetRecordBase> CreateRandomRecords(int eachTypes = 1, int levels = 1)
	{
		if (levels < 1)
		{
			yield break;
		}

		foreach (ValueRecord item in CreateValueRecords(eachTypes))
		{
			yield return item;
		}

		foreach (KeyValueRecord item in CreateKeyValueRecords(eachTypes))
		{
			yield return item;
		}

		foreach (RecordsGroup item in CreateGroups(eachTypes))
		{
			if (levels > 1)
			{
				item
					.Children
					.AddRange(CreateRandomRecords(eachTypes, --levels));
			}

			yield return item;
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="ValueRecord" /> objects.
	/// </summary>
	public static IEnumerable<ValueRecord> CreateValueRecords(int count)
	{
		string note = TextHelper
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new ValueRecord()
			{
				Value = AppUtils.CreateRandomString(10),
				Note = note
			};
		}
	}

	/// <inheritdoc cref="AddGroup" />
	public Task AddGroupAsync(
		string name,
		RecordsGroup? group,
		CancellationToken token = default)
	{
		_logger.LogInformation("Adding a records group");

		RecordsGroup record = new()
		{
			Name = name
		};

		AddInGroupOrInRecords(record, group);

		return SaveContentsAsync(token);
	}

	/// <inheritdoc cref="AddKeyValue" />
	public Task AddKeyValueAsync(
		string key,
		string? value,
		RecordsGroup? group,
		CancellationToken token = default)
	{
		_logger.LogInformation("Adding a key & value record");

		KeyValueRecord record = new()
		{
			Key = key,
			Value = value
		};

		AddInGroupOrInRecords(record, group);

		return SaveContentsAsync(token);
	}

	/// <inheritdoc cref="AddValue" />
	public Task AddValueAsync(
		string value,
		RecordsGroup? group,
		CancellationToken token = default)
	{
		_logger.LogInformation("Adding a value record");

		ValueRecord record = new()
		{
			Value = value
		};

		AddInGroupOrInRecords(record, group);

		return SaveContentsAsync(token);
	}

	/// <summary>
	/// Deletes a record from <see cref="Records" />.
	/// </summary>
	public Task DeleteRecordAsync(DatasetRecordBase record, CancellationToken token = default)
	{
		_logger.LogInformation("Deleting a record");

		DeleteRecord(Records, record);

		return SaveContentsAsync(token);
	}

	/// <inheritdoc cref="EditKeyValue" />
	public Task EditKeyValueAsync(
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

		return SaveContentsAsync(token);
	}

	/// <inheritdoc cref="EditNote(DatasetRecordBase?)" />
	public Task EditNoteAsync(
		DatasetRecordBase record,
		string? note,
		CancellationToken token = default)
	{
		_logger.LogInformation("Editing a note of a record");

		record.Note = note;

		return SaveContentsAsync(token);
	}

	/// <inheritdoc cref="EditValue(ValueRecord?)" />
	public Task EditValueAsync(
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

		return SaveContentsAsync(token);
	}

	/// <summary>
	/// Expands or collapses all <see cref="RecordsGroup" /> in <see cref="RecordsGroup.Children" /> or in <see cref="Records" /> depending on <paramref name="expand"/> value.
	/// </summary>
	public Task ExpandCollapseAsync(
		RecordsGroup? group,
		bool expand,
		CancellationToken token = default)
	{
		RecordsGroup[] groups = [.. (group is not null ? group.Children : Records)
			.Flatten()
			.OfType<RecordsGroup>()
			.Where(x => x.IsExpanded != expand)];

		if (groups.Length == 0)
		{
			return Task.CompletedTask;
		}

		groups.ForEach(x => x.IsExpanded = expand);

		_logger.LogInformation($"{(expand ? "Expand" : "Collapse")} all groups");

		if (IsReadOnly)
		{
			return Task.CompletedTask;
		}

		return SaveContentsAsync(token);
	}

	/// <summary>
	/// Loads data from the database.
	/// </summary>
	public async Task LoadDataAsync(ScrollViewer? scrollViewer = null, CancellationToken token = default)
	{
		if (IsInitialized)
		{
			return;
		}

		ContentsIsValidPair result = await _dbAccess
			.GetFileContentsAsync(FileId, token)
			.ConfigureAwait(false);

		if (result.IsDefault() || !result.IsValid)
		{
			_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{FileId}""");

			return;
		}

		try
		{
			if (result.Contents.Length == 0)
			{
				return;
			}

			string json = IFileEditor
				.Utf8Encoding
				.GetString(result.Contents);

			Records.AddRange(_jsonSerializer
				.Deserialize<DatasetRecordBase[]>(json)
				.AsNotNull());

			if (scrollViewer is null)
			{
				return;
			}

			// Virtualization is now disabled.
			await scrollViewer
				.WaitVirtualizingStackPanelIsLoadedAsync(token)
				.ConfigureAwait(true);

			await InitializePropertiesAsync(scrollViewer, token).ConfigureAwait(true);

			// The delay is necessary to avoid reacting to ScrollViewer events during initialization.
			await Task
				.Delay(300, token)
				.ConfigureAwait(true);

			Observable.FromEventPattern<ScrollChangedEventArgs>(
				x => scrollViewer.ScrollChanged += x,
				x => scrollViewer.ScrollChanged -= x)
				.Subscribe(ScrollViewer_ScrollChanged)
				.DisposeWith(_disposables);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			_logger.LogInformation($@"Content is initialized in ""{GetType().Name}""");

			IsInitialized = true;
		}
	}

	/// <inheritdoc cref="RenameGroup" />
	public Task RenameGroupAsync(
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

		return SaveContentsAsync(token);
	}

	/// <summary>
	/// Sets <see cref="ValueRecord.IsHidden" /> value from <paramref name="hide"/> to all records in <see cref="RecordsGroup" /> or in <see cref="Records" />.
	/// </summary>
	public Task ShowHideAsync(
		RecordsGroup? group,
		bool hide,
		CancellationToken token = default)
	{
		ValueRecord[] records = [.. (group is not null ? group.Children : Records)
			.Flatten()
			.OfType<ValueRecord>()
			.Where(x => x.IsHidden != hide)];

		if (records.Length == 0)
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation($"{(hide ? "Hide" : "Show")} all records");

		records.ForEach(x => x.IsHidden = hide);

		if (IsReadOnly)
		{
			return Task.CompletedTask;
		}

		return SaveContentsAsync(token);
	}

	/// <summary>
	/// Sorts <see cref="RecordsGroup" /> or <see cref="Records" /> in a required order.
	/// </summary>
	public Task SortAsync(
		RecordsGroup? group,
		ListSortDirection direction,
		CancellationToken token = default)
	{
		_logger.LogInformation("Sort records");

		ObservableCollection<DatasetRecordBase> records = group is not null
			? group.Children
			: Records;

		int beforeCount = records.GetCount();

		DatasetRecordBase[] sorted = records.Sort(direction);

		int afterCount = sorted.GetCount();

		if (afterCount != beforeCount)
		{
			return Task.CompletedTask;
		}

		records.Clear();

		records.AddRange(sorted);

		if (IsReadOnly)
		{
			return Task.CompletedTask;
		}

		return SaveContentsAsync(token);
	}
	#endregion

	#region Service
	/// <summary>
	/// Deletes a record from the sequence.
	/// </summary>
	private static void DeleteRecord(
		Collection<DatasetRecordBase> sequence,
		DatasetRecordBase record)
	{
		foreach (DatasetRecordBase item in sequence)
		{
			if (ReferenceEquals(item, record) && sequence.Remove(record))
			{
				return;
			}

			if (item is RecordsGroup group && group.Children.Count > 0)
			{
				DeleteRecord(group.Children, record);
			}
		}
	}

	/// <summary>
	/// Returns <c>True</c> if <see cref="RecordsGroup" /> has child objects.
	/// </summary>
	private static bool HasChildren(RecordsGroup? group) => group is not null && group.Children.Any();

	/// <summary>
	/// Returns <c>True</c> if <see cref="RecordsGroup" /> has child <see cref="RecordsGroup" />.
	/// </summary>
	private static bool HasGroups(RecordsGroup? group)
	{
		return group is not null && group.Children.OfType<RecordsGroup>().Any();
	}

	/// <summary>
	/// Adds <see cref="DatasetRecordBase" /> in <see cref="RecordsGroup" /> or in <see cref="Records" />
	/// if  <see cref="RecordsGroup" /> is null.
	/// </summary>
	private void AddInGroupOrInRecords(DatasetRecordBase record, RecordsGroup? group)
	{
		if (group is not null)
		{
			group.IsExpanded = true;

			group
				.Children
				.Add(record);
		}
		else
		{
			Records.Add(record);
		}
	}

	/// <inheritdoc cref="DeleteRecordAsync(DatasetRecordBase, CancellationToken)" />
	private Task<object?> DeleteRecordAsync(
		DatasetRecordBase record,
		string? questionText,
		CancellationToken token = default)
	{
		YesNoQuestionBox view = _viewLauncher.ConfigureYesNoQuestionBox($@"{Strings.Delete} ""{questionText}""?");

		view.ViewModel.DefaultPressedCallback = () =>
		{
			DialogHost.Close(null);

			return DeleteRecordAsync(record, token);
		};

		return DialogHost.Show(view);
	}

	/// <summary>
	/// Initializes <see cref="DatasetEditorViewModel" /> properties from database.
	/// </summary>
	private async Task InitializePropertiesAsync(ScrollViewer scrollViewer, CancellationToken token = default)
	{
		string? value = InitialProperties ?? await _dbAccess
			.GetFilePropertiesAsync(FileId, token)
			.ConfigureAwait(false);

		if (value is null)
		{
			return;
		}

		try
		{
			DatasetProperties properties = _jsonSerializer.Deserialize<DatasetProperties>(value);

			scrollViewer.Offset = new(default, properties.VerticalScrollOffset);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, false);
		}
	}

	/// <summary>
	/// Returns <c>True</c> if <see cref="Records" /> has elements.
	/// </summary>
	private bool IsAnyRecords() => Records.Any();

	/// <summary>
	/// Returns <c>True</c> if <see cref="EditorViewModelBase.IsReadOnly" /> is <c>False</c>.
	/// </summary>
	private bool IsNotReadOnly() => !IsReadOnly;

	/// <inheritdoc cref="FileEditorExtensions.SaveContentsAsync(IFileEditor, IDbAccess, ILogger, byte[], CancellationToken)" />
	private Task SaveContentsAsync(CancellationToken token = default)
	{
		return this.SaveContentsAsync(
			_dbAccess,
			_logger,
			IFileEditor.Utf8Encoding.GetBytes(_jsonSerializer.Serialize(Records)),
			token: token);
	}
	#endregion
}
