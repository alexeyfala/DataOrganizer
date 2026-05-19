using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="CopyHistoryView" />.
/// </summary>
public sealed partial class CopyHistoryViewModel : FileListViewModelBase
{
	#region Properties
	/// <summary>
	/// Search value within <see cref="Items" />.
	/// </summary>
	[ObservableProperty]
	public partial string? HistorySearch { get; set; }

	/// <summary>
	/// Returns <c>True</c> if <see cref="Items" /> is empty.
	/// </summary>
	public bool IsEmpty => _filter.IsSourceEmpty;

	/// <inheritdoc cref="CopyHistoryViewSettings.Items" />
	public ReadOnlyObservableCollection<FileModelDto> Items => _filter.Visible;

	/// <inheritdoc cref="CopyHistoryViewSettings.SelectedItemId" />
	[ObservableProperty]
	public partial FileModelDto? SelectedItem { get; set; }
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="SelectedItem" /> changes.
	/// </summary>
	partial void OnSelectedItemChanged(
		FileModelDto? oldValue,
		FileModelDto? newValue)
	{
		_previousSelectedItem = oldValue;
	}
	#endregion

	#region Data
	/// <summary>
	/// <inheritdoc cref="FilterEngine{T}" /> <see cref="Items" />.
	/// </summary>
	private readonly FilterEngine<FileModelDto> _filter;

	/// <summary>
	/// Previous <see cref="SelectedItem" /> value.
	/// </summary>
	private FileModelDto? _previousSelectedItem;
	#endregion

	#region Constructors
	public CopyHistoryViewModel(
		Application app,
		IClipboardService clipboard,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEntityEncryption entityEncryption,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler handler) : base(
			app,
			clipboard,
			dbAccess,
			dialogService,
			entityEncryption,
			logger,
			messenger,
			handler)
	{
		_filter = new(
			this.FilterPredicate(x => x.HistorySearch),
			autoRefreshOn: x => x.Name);

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
	/// <see cref="FilterEngine{TModel}.Visible" />.<see cref="INotifyCollectionChanged.CollectionChanged" /> event handler.
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
	/// Adds <see cref="FileModelDto" /> objects to the source.
	/// </summary>
	public void AddTestCopyHistory(IEnumerable<FileModelDto> items)
	{
		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			throw new InvalidOperationException("This method created for test purposes only, do not use it directly in code!");
		}

		_filter.AddRange(items);
	}

	/// <summary>
	/// Clears <see cref="Items" />.
	/// </summary>
	public void Clear()
	{
		HistorySearch = null;

		SelectedItem = null;

		_filter.Clear();
	}

	/// <summary>
	/// Returns a sequence of identifiers from <see cref="Items" />.
	/// </summary>
	public IEnumerable<Guid> GetIdentifiers() => _filter.SelectFromSource(x => x.Id);

	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(IEnumerable<FileModelDto> items, Guid selectedId)
	{
		_filter.AddRange(items);

		_filter.PostToUi(() => SelectedItem = _filter.FirstOrDefaultFromSource(x => x.Id == selectedId));
	}

	/// <summary>
	/// Inserts or moves to top value in <see cref="Items" />.
	/// </summary>
	public void InsertOrMoveToTop(FileModelDto file)
	{
		if (_filter.Contains(file))
		{
			_filter.Reorder(file, 0);
		}
		else
		{
			_filter.InsertAndRebuild(file, 0);
		}
	}

	/// <summary>
	/// Tries to remove value from <see cref="Items" />.
	/// </summary>
	public bool Remove(FileModelDto file) => _filter.Remove(file);

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		_filter.Dispose();

		SelectedItem = null;
	}
	#endregion
}
