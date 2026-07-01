using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Dataset;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
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
/// View model for <c>DatasetEditorView</c>.
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
	internal async Task ContainerLoaded(ItemsRepeater? container)
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

				SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

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

				SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

				return;
			}

			try
			{
				Records.AddRange(_jsonSerializer
					.Deserialize<DatasetRecordBase[]>(output)
					.AsNotNull());

				if (container?.FindAncestorOfType<ScrollViewer>() is not { } scrollViewer)
				{
					return;
				}

				await WaitItemsRepeaterRealizedAsync(container).ConfigureAwait(true);

				await InitializePropertiesAsync(scrollViewer, container).ConfigureAwait(true);

				SetupScrollSubscription(scrollViewer);
			}
			finally
			{
				output.ZeroMemory();
			}
		}
		catch (Exception ex)
		{
			IsContentCorrupted = true;

			_logger.LogException(ex, assertDebug: false);

			SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);
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
	internal Task IsHiddenChanged()
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
			MaskValueInput = IsEncrypted,
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
			KeyHint = Strings.Name,
			MaskKeyInput = IsEncrypted
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

		try
		{
			await _clipboard
				.SetTextAsync($"{record.Key}    {record.Value}")
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return;
		}

		record.PulseHighlight();
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
	private Task Expand(RecordsGroup? group) => ExpandCollapseAsync(group, true);

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
	[RelayCommand(CanExecute = nameof(CanScrollToEnd))]
	private Task ScrollToEnd(ScrollViewer? scrollViewer)
	{
		if (scrollViewer is null || Records.Count == 0)
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation("Scroll records to the end");

		//scrollViewer.Offset = new Vector(scrollViewer.Offset.X, scrollViewer.Extent.Height);

		return SmoothScrollAsync(scrollViewer, toEnd: true);
	}

	/// <summary>
	/// Scrolls the list to the top.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanScrollToTop))]
	private Task ScrollToTop(ScrollViewer? scrollViewer)
	{
		if (scrollViewer is null || Records.Count == 0)
		{
			return Task.CompletedTask;
		}

		_logger.LogInformation("Scroll records to the top");

		//scrollViewer.Offset = default;

		return SmoothScrollAsync(scrollViewer, toEnd: false);
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
	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <summary>
	/// Cached reference to the records <see cref="ItemsRepeater" />.
	/// </summary>
	private ItemsRepeater? _container;
	#endregion

	#region Constructors
	public DatasetEditorViewModel(
		Application app,
		IClipboardAccessor clipboardService,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IDispatcherAccessor dispatcher,
		IEntityEncryption entityEncryption,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler) : base(
			app,
			dbAccess,
			entityEncryption,
			jsonSerializer,
			logger,
			messenger,
			exceptionHandler)
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

		if (IsReadOnly || IsLastPropertiesEqualTo(json))
		{
			return;
		}

		_lastSavedProperties = json;

		_exceptionHandler.Watch(SavePropertiesAsync(json));
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

		AddInGroupOrInRecords(record, group);

		return SaveContentsAsync(token);
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

		AddInGroupOrInRecords(record, group);

		return SaveContentsAsync(token);
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

		AddInGroupOrInRecords(record, group);

		return SaveContentsAsync(token);
	}

	/// <summary>
	/// Deletes a record from <see cref="Records" />.
	/// </summary>
	internal Task DeleteRecordAsync(DatasetRecordBase record, CancellationToken token = default)
	{
		_logger.LogInformation("Deleting a record");

		DeleteRecord(Records, record);

		return SaveContentsAsync(token);
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

		return SaveContentsAsync(token);
	}

	/// <inheritdoc cref="EditNote(DatasetRecordBase?)" />
	internal Task EditNoteAsync(
		DatasetRecordBase record,
		string? note,
		CancellationToken token = default)
	{
		_logger.LogInformation("Editing a note of a record");

		record.Note = note;

		return SaveContentsAsync(token);
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

		return SaveContentsAsync(token);
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

		RecordsGroup[] groups = [.. source
			.Flatten()
			.OfType<RecordsGroup>()
			.Where(x => x.IsExpanded != expand)];

		if (groups.IsEmpty())
		{
			return;
		}

		_logger.LogInformation($"{(expand ? "Expand" : "Collapse")} all groups");

		await groups
			.ForEachAsync(x => _dispatcher.PostAsync(() => x.IsExpanded = expand, DispatcherPriority.Background))
			.ConfigureAwait(false);

		if (IsReadOnly || IsContentCorrupted)
		{
			return;
		}

		await SaveContentsAsync(token).ConfigureAwait(false);
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

		return SaveContentsAsync(token);
	}

	/// <summary>
	/// Sets <see cref="ValueRecord.IsHidden" /> value from <paramref name="hide"/> to all records in <see cref="RecordsGroup" /> or in <see cref="Records" />.
	/// </summary>
	internal Task ShowHideAsync(
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
	internal Task SortAsync(
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

	#region Helpers
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

	/// <summary>
	/// Smoothly scrolls <paramref name="scrollViewer" /> to the absolute top
	/// (<paramref name="toEnd" /> <c>False</c>) or bottom (<c>True</c>).
	/// </summary>
	private static Task SmoothScrollAsync(ScrollViewer scrollViewer, bool toEnd)
	{
		return StepOffsetUntilDoneAsync(scrollViewer, () =>
		{
			double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

			double targetY = toEnd ? maxY : 0;

			return targetY - scrollViewer.Offset.Y;
		});
	}

	/// <summary>
	/// Smoothly scrolls <paramref name="scrollViewer" /> so the record at
	/// <paramref name="topRecordIndex" /> sits with its top edge at
	/// <c>y = -<paramref name="withinRecordOffset" /></c> in viewport coordinates.
	/// </summary>
	private static async Task SmoothScrollAsync(
		ScrollViewer scrollViewer,
		ItemsRepeater container,
		int topRecordIndex,
		int totalRecords,
		double withinRecordOffset)
	{
		if (totalRecords <= 0)
		{
			return;
		}

		await StepOffsetUntilDoneAsync(scrollViewer, () =>
		{
			double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

			double targetY = Math.Clamp(
				((double)topRecordIndex / totalRecords * scrollViewer.Extent.Height) - withinRecordOffset,
				0,
				maxY);

			return targetY - scrollViewer.Offset.Y;
		}).ConfigureAwait(true);

		double targetViewportY = -withinRecordOffset;

		await StepOffsetUntilDoneAsync(scrollViewer, () =>
		{
			Control? child = container.TryGetElement(topRecordIndex) ?? container.GetOrCreateElement(topRecordIndex);

			if (child is null)
			{
				return 0;
			}

			container.UpdateLayout();

			if (child.TranslatePoint(default, scrollViewer) is not { } pointInViewport)
			{
				return 0;
			}

			return pointInViewport.Y - targetViewportY;
		}).ConfigureAwait(true);
	}

	/// <summary>
	/// Shared step-loop for the <see cref="SmoothScrollAsync" /> overloads.
	/// <paramref name="getRemainingDelta" /> returns signed pixels still to move
	/// (positive scrolls down).
	/// </summary>
	private static async Task StepOffsetUntilDoneAsync(ScrollViewer scrollViewer, Func<double> getRemainingDelta)
	{
		// Pixels moved per step. Smaller = more reliable realization, slower travel.
		const double StepPx = 100;

		// Pause between steps. Larger = more time for the layout pass to materialize items.
		const int DelayMs = 16;

		// Safety cap against runaway loops if the offset never converges (e.g. extent keeps growing).
		const int MaxIterations = 1000;

		for (int i = 0; i < MaxIterations; i++)
		{
			double delta = getRemainingDelta();

			if (Math.Abs(delta) < 0.5)
			{
				return;
			}

			double currentY = scrollViewer.Offset.Y;

			double step = Math.Sign(delta) * Math.Min(Math.Abs(delta), StepPx);

			double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

			double newY = Math.Clamp(currentY + step, 0, maxY);

			if (Math.Abs(newY - currentY) < 0.5)
			{
				return;
			}

			scrollViewer.Offset = new Vector(scrollViewer.Offset.X, newY);

			await Task
				.Delay(DelayMs)
				.ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Returns the data index and pixel offset of the realized record whose top edge
	/// is the closest one at or above the <paramref name="scrollViewer" /> viewport
	/// top, or <c>null</c> if no realized child qualifies.
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
	/// Waits until <paramref name="container" /> has realized at least one child.
	/// </summary>
	private static async Task<bool> WaitItemsRepeaterRealizedAsync(
		ItemsRepeater container,
		CancellationToken token = default)
	{
		Func<bool> condition = () => container
			.Children
			.Count > 0;

		await condition
			.WaitAsync(400, 10, token)
			.ConfigureAwait(true);

		return container
			.Children
			.Count > 0;
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

	/// <summary>
	/// Validates <see cref="ScrollToEndCommand" />.
	/// </summary>
	private bool CanScrollToEnd() => !ScrollToTopCommand.IsRunning;

	/// <summary>
	/// Validates <see cref="ScrollToTopCommand" />.
	/// </summary>
	private bool CanScrollToTop() => !ScrollToEndCommand.IsRunning;

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

			await SmoothScrollAsync(
				scrollViewer,
				container,
				properties.TopRecordIndex,
				Records.Count,
				properties.WithinRecordOffset).ConfigureAwait(false);

			// Just in case.
#pragma warning disable CS8321 // Local function is declared but never used
			void RestoreScroll()
			{
				Control? child = container.TryGetElement(properties.TopRecordIndex) ?? container.GetOrCreateElement(properties.TopRecordIndex);

				if (child is null)
				{
					return;
				}

				container.UpdateLayout();

				if (child.TranslatePoint(default, scrollViewer) is not { } pointInViewport)
				{
					return;
				}
				double targetViewportY = -properties.WithinRecordOffset;

				double delta = pointInViewport.Y - targetViewportY;

				double maxOffsetY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

				scrollViewer.Offset = new Vector(
					scrollViewer.Offset.X,
					Math.Clamp(scrollViewer.Offset.Y + delta, 0, maxOffsetY));
			}
#pragma warning restore CS8321 // Local function is declared but never used
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, false);
		}
	}

	/// <summary>
	/// <c>True</c> when <see cref="Records" /> has elements.
	/// </summary>
	private bool IsAnyRecords() => Records.Any();

	/// <summary>
	/// <c>True</c> when <see cref="EmbeddedEditorViewModelBase.IsReadOnly" /> is <c>False</c> and <see cref="EmbeddedEditorViewModelBase.IsContentCorrupted" /> is <c>False</c>.
	/// </summary>
	private bool IsNotReadOnlyNotCorrupted() => !IsReadOnly && !IsContentCorrupted;

	/// <inheritdoc cref="EmbeddedEditorViewModelBase.SaveContentsAsync" />
	private async Task SaveContentsAsync(CancellationToken token = default)
	{
		byte[] contents = _jsonSerializer.SerializeToUtf8Bytes(Records);

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

	/// <summary>
	/// Wires up the debounced subscription to <see cref="ScrollViewer.ScrollChanged" />
	/// on <paramref name="scrollViewer" />, re-attaching the handler whenever the
	/// scroll viewer is detached / re-attached to the visual tree, and disposing
	/// everything with the view-model's lifetime.
	/// </summary>
	private void SetupScrollSubscription(ScrollViewer scrollViewer)
	{
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
					.SetDelay(TimeSpan.FromSeconds(0.5), false)
					.Subscribe(ScrollViewer_ScrollChanged);
			}, DispatcherPriority.Loaded);
		}
	}
	#endregion
}
