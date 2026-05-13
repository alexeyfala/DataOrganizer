using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Models;
using DataOrganizer.Views;
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
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="DatasetEditorView" />.
/// </summary>
public sealed partial class DatasetEditorViewModel : EmbeddedEditorViewModelBase
{
	#region Properties
	/// <summary>
	/// Records.
	/// </summary>
	public ObservableCollection<DatasetRecordBase> Records { get; } = [];
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Handles the <see cref="Control.Loaded" /> event of <see cref="ItemsRepeater" />.
	/// </summary>
	[RelayCommand]
	public async Task ContainerLoaded(ItemsRepeater? container)
	{
		if (IsInitialized)
		{
			return;
		}

		ContentsIsValidPair result = await _dbAccess
			.GetFileContentsAsync(FileId)
			.ConfigureAwait(true);

		try
		{
			if (!result.IsValid)
			{
				IsContentCorrupted = true;

				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

				_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{FileId}""");

				return;
			}

			_container = container;

			if (result
				.Contents
				.IsEmpty())
			{
				return;
			}

			if (TryToDecrypt(result.Contents) is not { } output)
			{
				IsContentCorrupted = true;

				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

				return;
			}

			try
			{
				string json = TextHelper
					.Utf8Encoding
					.GetString(output);

				Records.AddRange(_jsonSerializer
					.Deserialize<DatasetRecordBase[]>(json)
					.AsNotNull());

				if (container is null || container?.FindAncestorOfType<ScrollViewer>() is not { } scrollViewer)
				{
					return;
				}

				await WaitVirtualizingStackPanelIsLoadedAsync(scrollViewer).ConfigureAwait(true);

				await InitializePropertiesAsync(scrollViewer, container).ConfigureAwait(true);

				SerialDisposable scrollSubscription = new();

				scrollSubscription.DisposeWith(_disposables);

				bool isAttached = true;

				scrollViewer.DetachedFromVisualTree += ScrollViewer_DetachedFromVisualTree;

				scrollViewer.AttachedToVisualTree += ScrollViewer_AttachedToVisualTree;

				Disposable.Create(() =>
				{
					scrollViewer.DetachedFromVisualTree -= ScrollViewer_DetachedFromVisualTree;
					scrollViewer.AttachedToVisualTree -= ScrollViewer_AttachedToVisualTree;
				}).DisposeWith(_disposables);

				SubscribeToScrollChanged();

				void ScrollViewer_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
				{
					isAttached = false;

					scrollSubscription.Disposable = null;
				}

				void ScrollViewer_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
				{
					isAttached = true;

					SubscribeToScrollChanged();
				}

				void SubscribeToScrollChanged()
				{
					if (!isAttached)
					{
						return;
					}

					_dispatcher.Post(() =>
					{
						scrollSubscription.Disposable = Observable.FromEventPattern<EventHandler<ScrollChangedEventArgs>, ScrollChangedEventArgs>(
							x => scrollViewer.ScrollChanged += x,
							x => scrollViewer.ScrollChanged -= x)
							.SetDelay(TimeSpan.FromSeconds(0.3), false)
							.Subscribe(ScrollViewer_ScrollChanged);
					}, DispatcherPriority.Loaded);
				}
			}
			finally
			{
				output.ZeroMemory();
			}
		}
		catch (Exception ex)
		{
			IsContentCorrupted = true;

			_logger.LogException(ex, isAssertDebug: false);

			_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));
		}
		finally
		{
			IsInitialized = true;

			_logger.LogInformation($@"Content is initialized in ""{GetType().Name}""");
		}
	}

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
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
	private async Task AddGroup(RecordsGroup? group)
	{
		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.AddGroup,
			KeyHint = Strings.Name
		};

		if (await _dialogService
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
	private async Task AddKeyValue(RecordsGroup? group)
	{
		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.AddKeyAndValue,
			KeyHint = Strings.Key,
			ValueHint = Strings.Value
		};

		if (await _dialogService
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
	private async Task AddValue(RecordsGroup? group)
	{
		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.AddValue,
			KeyHint = Strings.Name
		};

		if (await _dialogService
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
	private Task Collapse(RecordsGroup? group) => ExpandCollapseAsync(group, true);

	/// <summary>
	/// Copies <see cref="KeyValueRecord" /> key and value to system clipboard.
	/// </summary>
	[RelayCommand]
	private async Task CopyKeyValueToClipboard(KeyValueRecord? record)
	{
		if (record is null || !await _clipboard
			.SetTextAsync($"{record.Key}    {record.Value}")
			.ConfigureAwait(true))
		{
			return;
		}

		record.IsHighlight = true;

		record.IsHighlight = false;
	}

	/// <summary>
	/// Deletes a <see cref="RecordsGroup" /> from <see cref="Records" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
	private async Task EditKeyValue(KeyValueRecord? record)
	{
		if (record is null)
		{
			return;
		}

		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.Save,
			Key = record.Key,
			KeyHint = Strings.Key,
			Value = record.Value,
			ValueHint = Strings.Value
		};

		if (await _dialogService
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
	private async Task EditNote(DatasetRecordBase? record)
	{
		if (record is null)
		{
			return;
		}

		ValueIsValidPair result = await _dialogService
			.RequestMultilineTextAsync(record.Note)
			.ConfigureAwait(false);

		if (!result.IsValid)
		{
			return;
		}

		await EditNoteAsync(record, result.Value).ConfigureAwait(false);
	}

	/// <summary>
	/// Edits a <see cref="ValueRecord" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
	private async Task EditValue(ValueRecord? record)
	{
		if (record is null)
		{
			return;
		}

		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.Save,
			Key = record.Value,
			KeyHint = Strings.Edit
		};

		if (await _dialogService
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
	private Task Expand(RecordsGroup? group) => ExpandCollapseAsync(group, false);

	/// <summary>
	/// Expands or collapses all <see cref="RecordsGroup" /> in <see cref="Records" /> depending on <paramref name="expand"/> value.
	/// </summary>
	[RelayCommand]
	private Task ExpandCollapse(bool expand) => ExpandCollapseAsync(null, expand);

	/// <summary>
	/// Handles the <see cref="Expander.Expanded" />, <see cref="Expander.Collapsed" /> events by user.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
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
	[RelayCommand(CanExecute = nameof(IsNotReadOnlyNotCorrupted))]
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

		if (await _dialogService
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(false) is not { } result)
		{
			return;
		}

		await RenameGroupAsync(group, result.Key).ConfigureAwait(false);
	}

	/// <summary>
	/// Scrolls the list to the end.
	/// </summary>
	[RelayCommand]
	private void ScrollToEnd(ItemsRepeater? container)
	{
		if (container?.FindAncestorOfType<ScrollViewer>() is not { } scrollViewer)
		{
			return;
		}

		_logger.LogInformation("Scroll records to the end");

		scrollViewer.Offset = new Vector(scrollViewer.Offset.X, scrollViewer.Extent.Height);

		if (Records.Count == 0)
		{
			return;
		}

		container.GetOrCreateElement(Records.Count - 1);
	}

	/// <summary>
	/// Scrolls the list to the top.
	/// </summary>
	[RelayCommand]
	private void ScrollToTop(ItemsRepeater? container)
	{
		if (container?.FindAncestorOfType<ScrollViewer>() is not { } scrollViewer)
		{
			return;
		}

		_logger.LogInformation("Scroll records to the top");

		scrollViewer.Offset = default;

		if (Records.Count == 0)
		{
			return;
		}

		container.GetOrCreateElement(0);
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

	/// <summary>
	/// Sorts <see cref="RecordsGroup" /> child objects in <see cref="ListSortDirection.Ascending" /> order.
	/// </summary>
	[RelayCommand(CanExecute = nameof(HasChildren))]
	private async Task SortAscending(RecordsGroup? group)
	{
		if (!await _dialogService
			.RequestYesNoDialogAsync(Strings.SortAscending + "?")
			.ConfigureAwait(false))
		{
			return;
		}

		await SortAsync(group, ListSortDirection.Ascending).ConfigureAwait(false);
	}

	/// <summary>
	/// Sorts <see cref="Records" /> ascending or descending order.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsAnyRecords))]
	private async Task SortAscendingDescending(ListSortDirection direction)
	{
		string text = string.Concat(direction switch
		{
			ListSortDirection.Ascending => Strings.SortAscending,
			ListSortDirection.Descending => Strings.SortDescending,
			_ => throw new NotImplementedException()
		}, "?");

		if (!await _dialogService
			.RequestYesNoDialogAsync(text)
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
		if (!await _dialogService
			.RequestYesNoDialogAsync(Strings.SortDescending + "?")
			.ConfigureAwait(false))
		{
			return;
		}

		await SortAsync(group, ListSortDirection.Descending).ConfigureAwait(false);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IClipboardService" />
	private readonly IClipboardService _clipboard;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <summary>
	/// Cached reference to the records <see cref="ItemsRepeater" />.
	/// </summary>
	private ItemsRepeater? _container;
	#endregion

	#region Constructors
	public DatasetEditorViewModel(
		Application app,
		IClipboardService clipboardService,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IDispatcher dispatcher,
		IEntityEncryption entityEncryption,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		ITaskExceptionHandler handler,
		IViewModelExecutionService viewModel) : base(
			app,
			dbAccess,
			entityEncryption,
			jsonSerializer,
			logger,
			handler,
			viewModel)
	{
		_clipboard = clipboardService;

		_dialogService = dialogService;

		_dispatcher = dispatcher;
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
			if (IsContentCorrupted || e.Sender is not ScrollViewer scrollViewer || _container is null)
			{
				return;
			}

			// Skip layout-driven events (Extent/Viewport changed but not Offset).
			if (e.EventArgs.OffsetDelta == default)
			{
				return;
			}

			if (TryGetTopVisibleRecord(_container, scrollViewer) is not { } anchor)
			{
				return;
			}

			DatasetProperties properties = new()
			{
				TopRecordIndex = anchor.Index,
				WithinRecordOffset = anchor.WithinRecordOffset
			};

			string json = _jsonSerializer.Serialize(properties, AppUtils.JsonOptions);

			SetPropertiesCallback?.Invoke(json);

			if (IsReadOnly)
			{
				return;
			}

			_handler.Watch(SavePropertiesAsync(json));
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
				Name = $"Group_{AppUtils.CreateRandomString(10)}",
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
				Key = $"Key_{AppUtils.CreateRandomString(10)}",
				Value = $"Value_{AppUtils.CreateRandomString(10)}",
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
					.AddRange(CreateRandomRecords(eachTypes, levels - 1));
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
				Value = $"Value_{AppUtils.CreateRandomString(10)}",
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
	public async Task ExpandCollapseAsync(
		RecordsGroup? group,
		bool expand,
		CancellationToken token = default)
	{
		RecordsGroup[] groups = [.. (group is not null ? group.Children : Records)
			.Flatten()
			.OfType<RecordsGroup>()
			.Where(x => x.IsExpanded != expand)];

		if (groups.IsEmpty())
		{
			return;
		}

		_logger.LogInformation($"{(expand ? "Expand" : "Collapse")} all groups");

		await groups
			.ForEachAsync(x => x.IsExpanded = expand, Environment.ProcessorCount, token)
			.ConfigureAwait(false);

		if (IsReadOnly || IsContentCorrupted)
		{
			return;
		}

		await SaveContentsAsync(token).ConfigureAwait(false);
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

		if (records.IsEmpty())
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation($"{(hide ? "Hide" : "Show")} all records");

		records.ForEach(x => x.IsHidden = hide);

		if (IsReadOnly || IsContentCorrupted)
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

		if (IsReadOnly || IsContentCorrupted)
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
	private static bool HasChildren(RecordsGroup? group)
	{
		return group?
			.Children
			.Any() == true;
	}

	/// <summary>
	/// Returns <c>True</c> if <see cref="RecordsGroup" /> has child <see cref="RecordsGroup" />.
	/// </summary>
	private static bool HasGroups(RecordsGroup? group)
	{
		return group?
			.Children
			.OfType<RecordsGroup>()
			.Any() == true;
	}

	/// <summary>
	/// Iteratively adjusts <paramref name="scrollViewer" /> offset until the record at
	/// <paramref name="topRecordIndex" /> lands with its top edge at
	/// <c>y = -<paramref name="withinRecordOffset" /></c> in the viewport.
	/// Iteration is needed because in ProItemsRepeater's logical-scrolling mode each
	/// offset change triggers a re-layout that can nudge the anchor element.
	/// </summary>
	private static void RestoreScrollPosition(
		ScrollViewer scrollViewer,
		ItemsRepeater container,
		int topRecordIndex,
		double withinRecordOffset)
	{
		container.UpdateLayout();

		double targetViewportY = -withinRecordOffset;

		for (int attempt = 0; attempt < 8; attempt++)
		{
			Control? child = container.TryGetElement(topRecordIndex) ?? container.GetOrCreateElement(topRecordIndex);

			if (child is null)
			{
				return;
			}

			container.UpdateLayout();

			if (child.TranslatePoint(default, scrollViewer) is not { } pointInViewport)
			{
				return;
			}

			double delta = pointInViewport.Y - targetViewportY;

			if (Math.Abs(delta) < 0.5)
			{
				return;
			}

			double maxOffsetY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

			double newOffsetY = Math.Clamp(scrollViewer.Offset.Y + delta, 0, maxOffsetY);

			if (Math.Abs(newOffsetY - scrollViewer.Offset.Y) < 0.5)
			{
				return;
			}

			scrollViewer.Offset = new Vector(scrollViewer.Offset.X, newOffsetY);
		}
	}

	/// <summary>
	/// Returns the data index and pixel offset of the realized record whose top edge
	/// is the closest one at or above the <paramref name="scrollViewer" /> viewport
	/// top, or <c>null</c> if no realized child qualifies.
	/// <see cref="Visual.TranslatePoint" /> is used rather than <c>Bounds.Top</c>
	/// because ProItemsRepeater renders children in viewport-relative coordinates in
	/// logical-scrolling mode; the transform tree gives a correct value in both modes.
	/// </summary>
	private static (int Index, double WithinRecordOffset)? TryGetTopVisibleRecord(
		ItemsRepeater container,
		ScrollViewer scrollViewer)
	{
		int topIndex = -1;

		double bestViewportY = double.NegativeInfinity;

		foreach (Control child in container.Children)
		{
			int index = container.GetElementIndex(child);

			if (index < 0)
			{
				continue;
			}

			if (child.TranslatePoint(default, scrollViewer) is not { } pointInViewport)
			{
				continue;
			}

			double viewportY = pointInViewport.Y;

			if (viewportY <= 0 && viewportY > bestViewportY)
			{
				bestViewportY = viewportY;

				topIndex = index;
			}
		}

		return topIndex < 0 ? null : (topIndex, -bestViewportY);
	}

	/// <summary>
	/// Finds the <see cref="" /> within the visual tree and waits until it is loaded.
	/// </summary>
	private static async Task<bool> WaitVirtualizingStackPanelIsLoadedAsync(
		Visual element,
		CancellationToken token = default)
	{
		if (element.FindDescendantOfType<VirtualizingStackPanel>(includeSelf: false) is not { } panel)
		{
			return false;
		}

		Func<bool> condition = () => panel.IsLoaded;

		await condition
			.WaitAsync(300, 10, token)
			.ConfigureAwait(true);

		return panel.IsLoaded;
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
	private async Task DeleteRecordAsync(
		DatasetRecordBase record,
		string? questionText,
		CancellationToken token = default)
	{
		if (!await _dialogService
			.RequestYesNoDialogAsync($@"{Strings.Delete} ""{questionText}""?", token)
			.ConfigureAwait(false))
		{
			return;
		}

		await DeleteRecordAsync(record, token).ConfigureAwait(false);
	}

	/// <summary>
	/// Initializes <see cref="DatasetEditorViewModel" /> properties from database.
	/// </summary>
	private async Task InitializePropertiesAsync(
		ScrollViewer scrollViewer,
		ItemsRepeater container,
		CancellationToken token = default)
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

			if (properties.TopRecordIndex < 0 || properties.TopRecordIndex >= Records.Count)
			{
				return;
			}

			RestoreScrollPosition(
				scrollViewer,
				container,
				properties.TopRecordIndex,
				properties.WithinRecordOffset);
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
	/// Returns <c>True</c> if <see cref="EmbeddedEditorViewModelBase.IsReadOnly" /> is <c>False</c> and <see cref="EmbeddedEditorViewModelBase.IsContentCorrupted" /> is <c>False</c>.
	/// </summary>
	private bool IsNotReadOnlyNotCorrupted() => !IsReadOnly && !IsContentCorrupted;

	/// <inheritdoc cref="EmbeddedEditorViewModelBase.SaveContentsAsync" />
	private async Task SaveContentsAsync(CancellationToken token = default)
	{
		byte[] contents = TextHelper
			.Utf8Encoding
			.GetBytes(_jsonSerializer.Serialize(Records));

		if (TryToEncrypt(contents) is not { } output)
		{
			_logger.LogError($@"{Strings.FailedToProcessContents} of file ""{FileId}""");

			return;
		}

		try
		{
			await SaveContentsAsync(
				output,
				token: token).ConfigureAwait(false);
		}
		finally
		{
			contents.ZeroMemory();

			output.ZeroMemory();
		}
	}
	#endregion
}
