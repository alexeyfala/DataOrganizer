using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DynamicData.Binding;
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
public sealed partial class CopyHistoryViewModel : FileListViewModel, IDisposable
{
	#region Properties
	/// <inheritdoc cref="CopyHistoryViewSettings.CopyHistory" />
	public ReadOnlyObservableCollection<FileModelDto> CopyHistory => _filter.Visible;

	/// <summary>
	/// Returns <c>True</c> if <see cref="CopyHistory" /> is empty.
	/// </summary>
	public bool IsCopyHistoryEmpty => _filter.IsEmpty;
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Search value within <see cref="CopyHistory" />.
	/// </summary>
	[ObservableProperty]
	private string? _historySearch;

	/// <inheritdoc cref="CopyHistoryViewSettings.SelectedCopyHistoryItemId" />
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

	#region Auto-Generated Commands
	/// <summary>
	/// Clears <see cref="CopyHistory" />.
	/// </summary>
	[RelayCommand]
	public void Clear()
	{
		HistorySearch = null;

		SelectedItem = null;

		_filter.Clear();
	}
	#endregion

	#region Data
	/// <summary>
	/// <inheritdoc cref="FilterEngine{T}" /> <see cref="CopyHistory" />.
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
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEncryptionService encryption,
		IEntityEcryption entityEcryption,
		ILogger logger) : base(
			app,
			dbAccess,
			dialogService,
			encryption,
			entityEcryption,
			logger)
	{
		IObservable<Func<IName, bool>> predicate = this.FilterPredicate(
			x => x.HistorySearch,
			HistorySearch,
			HistorySearchEmptyStringAction);

		_filter = new(
			predicate,
			SortExpressionComparer<FileModelDto>.Ascending(x => x.Order));
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

	/// <inheritdoc />
	public void Dispose()
	{
		_logger.LogInformation($"Disposing: {GetType().Name}");

		_filter.Dispose();

		SelectedItem = null;
	}

	/// <summary>
	/// Returns a sequence of identifiers from <see cref="CopyHistory" />.
	/// </summary>
	public IEnumerable<Guid> GetIdentifiers() => _filter.Select(x => x.Id);

	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(IEnumerable<FileModelDto> items, Guid selectedId)
	{
		_filter.AddRange(items);

		_filter.Synchronize(() => SelectedItem = _filter.FirstOrDefault(x => x.Id == selectedId));
	}

	/// <summary>
	/// Sets <see cref="SelectedItem" /> from <paramref name="item"/> using <see cref="FilterEngine{FileModelDto}.Synchronize" />.
	/// </summary>
	public void SetSelectedItem(FileModelDto item) => _filter.Synchronize(() => SelectedItem = item);
	#endregion

	#region Service
	/// <summary>
	/// The action called when <see cref="HistorySearch" /> has empty string value.
	/// </summary>
	private void HistorySearchEmptyStringAction()
	{
		RefreshFilter();

		SelectedItem ??= _previousSelectedItem;
	}

	/// <summary>
	/// Refreshes filter for <see cref="CopyHistory" />.
	/// </summary>
	private void RefreshFilter() => _filter.IterateSource((x, i) => x.Order = i);
	#endregion
}
