using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
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

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="CopyHistoryView" />.
/// </summary>
public sealed partial class CopyHistoryViewModel : FileListViewModelBase, IDisposable
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if <see cref="Items" /> is empty.
	/// </summary>
	public bool IsEmpty => _filter.IsSourceEmpty;

	/// <inheritdoc cref="CopyHistoryViewSettings.Items" />
	public ReadOnlyObservableCollection<FileModelDto> Items => _filter.Visible;
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Search value within <see cref="Items" />.
	/// </summary>
	[ObservableProperty]
	private string? _historySearch;

	/// <inheritdoc cref="CopyHistoryViewSettings.SelectedItemId" />
	[ObservableProperty]
	private FileModelDto? _selectedItem;
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
		IEntityEcryption entityEcryption,
		ILogger logger,
		ITaskExceptionHandler handler,
		IViewModelExecutionService viewModel) : base(
			app,
			clipboard,
			dbAccess,
			dialogService,
			entityEcryption,
			logger,
			handler,
			viewModel)
	{
		IObservable<Func<IName, bool>> predicate = this.FilterPredicate(
			x => x.HistorySearch,
			HistorySearch,
			HistorySearchEmptyStringAction);

		_filter = new(predicate, autoRefreshOn: x => x.Name);
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

	/// <inheritdoc />
	public void Dispose()
	{
		_logger.LogInformation($"Disposing: {GetType().Name}");

		_filter.Dispose();

		SelectedItem = null;
	}

	/// <summary>
	/// Returns a sequence of identifiers from <see cref="Items" />.
	/// </summary>
	public IEnumerable<Guid> GetIdentifiers() => _filter.Select(x => x.Id);

	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(IEnumerable<FileModelDto> items, Guid selectedId)
	{
		_filter.AddRange(items);

		_filter.PostToUi(() => SelectedItem = _filter.FirstOrDefault(x => x.Id == selectedId));
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
	#endregion

	#region Service
	/// <summary>
	/// The action called when <see cref="HistorySearch" /> has empty string value.
	/// </summary>
	private void HistorySearchEmptyStringAction() => SelectedItem ??= _previousSelectedItem;
	#endregion
}
