using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Collections;
using DataOrganizer.Extensions.Hierarchy;
using DataOrganizer.Helpers.Collections;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Messages.Clipboard;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>CopyHistoryView</c>.
/// </summary>
public sealed partial class CopyHistoryViewModel :
	FileListViewModelBase,
	IRecipient<CopyHistoryClearedMessage>,
	IRecipient<CopyHistoryFileAddedMessage>,
	IRecipient<CopyHistoryFileRemovedMessage>
{
	#region Properties
	/// <summary>
	/// Search value within <see cref="Items" />.
	/// </summary>
	[ObservableProperty]
	public partial string? HistorySearch { get; set; }

	/// <summary>
	/// <c>True</c> when <see cref="Items" /> is empty.
	/// </summary>
	public bool IsEmpty => _filter.IsSourceEmpty;

	/// <summary>
	/// The copy-history files that pass the current filter.
	/// </summary>
	public ReadOnlyObservableCollection<FileDto> Items => _filter.Visible;

	/// <inheritdoc cref="ICopyHistory.SelectedItemId" />
	[ObservableProperty]
	public partial FileDto? SelectedItem { get; set; }
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="SelectedItem" /> changes.
	/// </summary>
	partial void OnSelectedItemChanged(
		FileDto? oldValue,
		FileDto? newValue)
	{
		_previousSelectedItem = oldValue;

		// A disposed list lets go of its files, which is not a choice.
		if (IsDisposed)
		{
			return;
		}

		_copyHistory.SelectedItemId = newValue?.Id ?? default;
	}
	#endregion

	#region Data
	/// <inheritdoc cref="ICopyHistory" />
	private readonly ICopyHistory _copyHistory;

	/// <summary>
	/// <inheritdoc cref="FilteredCollection{T}" /> <see cref="Items" />.
	/// </summary>
	private readonly FilteredCollection<FileDto> _filter;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <summary>
	/// Previous <see cref="SelectedItem" /> value.
	/// </summary>
	private FileDto? _previousSelectedItem;
	#endregion

	#region Constructors
	public CopyHistoryViewModel(
		IContentCopier contentCopier,
		ICopyHistory copyHistory,
		IMessenger messenger,
		ISessionHierarchy sessionHierarchy,
		ITaskExceptionHandler exceptionHandler) : base(
			contentCopier,
			messenger,
			exceptionHandler)
	{
		_copyHistory = copyHistory;

		_messenger = messenger;

		_filter = new(
			this.FilterPredicate(x => x.HistorySearch),
			autoRefreshOn: x => x.Name);

		_filter.AddRange(sessionHierarchy.Hierarchy.FilterFilesById(copyHistory.ItemIds));

		Guid selectedId = copyHistory.SelectedItemId;

		_filter.PostToUi(() => SelectedItem = _filter.FirstOrDefaultFromSource(x => x.Id == selectedId));

		messenger.RegisterAll(this);

		if (_filter.Visible is not INotifyCollectionChanged notifier)
		{
			return;
		}

		notifier.CollectionChanged += Filter_CollectionChanged;

		Disposable
			.Create(() => notifier.CollectionChanged -= Filter_CollectionChanged)
			.DisposeWith(_disposables);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Visible" />.<see cref="INotifyCollectionChanged.CollectionChanged" /> event handler.
	/// </summary>
	private void Filter_CollectionChanged(
		object? sender,
		NotifyCollectionChangedEventArgs e)
	{
		if (SelectedItem is not null
			|| _previousSelectedItem is null
			|| !_filter.Visible.Contains(_previousSelectedItem))
		{
			return;
		}

		SelectedItem = _previousSelectedItem;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Empties the list, its search and its choice.
	/// </summary>
	public void Receive(CopyHistoryClearedMessage message)
	{
		HistorySearch = null;

		SelectedItem = null;

		_filter.Clear();
	}

	/// <summary>
	/// Inserts the copied file at the top of the list or moves it there.
	/// </summary>
	public void Receive(CopyHistoryFileAddedMessage message)
	{
		if (_filter.Contains(message.File))
		{
			_filter.Reorder(message.File, 0);
		}
		else
		{
			_filter.InsertAndRebuild(message.File, 0);
		}
	}

	/// <summary>
	/// Removes the file from the list.
	/// </summary>
	public void Receive(CopyHistoryFileRemovedMessage message) => _filter.Remove(message.File);

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		_messenger.UnregisterAll(this);

		_filter.Dispose();

		SelectedItem = null;
	}

	/// <inheritdoc />
	protected override void SelectFile(FileDto file) => SelectedItem = file;
	#endregion
}
