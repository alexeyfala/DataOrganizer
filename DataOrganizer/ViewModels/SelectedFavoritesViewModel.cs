using Clipboard.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.DragAndDrop;
using DataOrganizer.Extensions.Collections;
using DataOrganizer.Helpers.Collections;
using DataOrganizer.Helpers.Views;
using Diagnostics.Interfaces;
using Favorites.Dto;
using Hierarchy.Dto;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>SelectedFavoritesView</c>.
/// </summary>
public sealed partial class SelectedFavoritesViewModel : FileListViewModelBase
{
	#region Properties
	/// <inheritdoc cref="FavoritesViewSettings.Categories" />
	public ReadOnlyObservableCollection<FavoriteCategory> Categories => _categoriesFilter.Visible;

	/// <summary>
	/// Search value within <see cref="Categories" />.
	/// </summary>
	[ObservableProperty]
	public partial string? CategorySearch { get; set; }

	/// <summary>
	/// A sequence of <see cref="FileDto" />.
	/// </summary>
	public ReadOnlyObservableCollection<FileDto> Favorites => _favoritesFilter.Visible;

	/// <summary>
	/// Search value within <see cref="Favorites" />.
	/// </summary>
	[ObservableProperty]
	public partial string? FavoriteSearch { get; set; }

	/// <summary>
	/// <c>True</c> when <see cref="Categories" /> is empty.
	/// </summary>
	public bool IsCategoriesEmpty => _categoriesFilter.IsSourceEmpty;

	/// <summary>
	/// <c>True</c> when <see cref="Favorites" /> is empty.
	/// </summary>
	public bool IsFavoritesEmpty => _favoritesFilter.IsSourceEmpty;

	/// <summary>
	/// Width of the navigation column.
	/// </summary>
	[ObservableProperty]
	public partial double NavigationColumnWidth { get; set; }

	/// <inheritdoc cref="FavoritesViewSettings.OrderedCategoryIds" />
	public List<Guid> OrderedCategoryIds { get; } = [];

	/// <summary>
	/// The selected object in the <see cref="Categories" />.
	/// </summary>
	[ObservableProperty]
	public partial FavoriteCategory? SelectedCategory { get; set; }

	/// <summary>
	/// The selected object in the <see cref="Favorites" />.
	/// </summary>
	[ObservableProperty]
	public partial FileDto? SelectedFavorite { get; set; }

	/// <inheritdoc cref="FavoritesViewSettings.SelectedPairs" />
	public List<FavoriteSelection> SelectedPairs { get; } = [];
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="SelectedCategory" /> changes.
	/// </summary>
	partial void OnSelectedCategoryChanged(
		FavoriteCategory? oldValue,
		FavoriteCategory? newValue)
	{
		_previousSelectedCategory = oldValue;

		_favoritesFilter.Clear();

		if (newValue is null)
		{
			return;
		}

		_favoritesFilter.AddRange(newValue.Children);

		if (SelectedPairs.FirstOrDefault(x => x.CategoryId == newValue.Id) is not { } pair)
		{
			return;
		}

		if (_favoritesFilter.FirstOrDefaultFromSource(x => x.Id == pair.FavoriteId) is not { } favorite)
		{
			return;
		}

		_favoritesFilter.PostToUi(() => SelectedFavorite = favorite);
	}

	/// <summary>
	/// Called when <see cref="SelectedFavorite" /> changes.
	/// </summary>
	partial void OnSelectedFavoriteChanged(FileDto? oldValue, FileDto? newValue)
	{
		_previousSelectedFavorite = oldValue;

		if (newValue is null)
		{
			return;
		}

		AddOrUpdateSelectedPairs(newValue.Id);
	}
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Handles when item in <see cref="Categories" /> has dragged.
	/// </summary>
	[RelayCommand]
	private void CategoryDragged(IndexMove pair)
	{
		FavoriteCategory selected = Categories[pair.DraggedIndex];

		_categoriesFilter.Reorder(selected, pair.TargetIndex);

		_categoriesFilter.PostToUi(() => SelectedCategory = selected);

		OrderedCategoryIds.ClearAddRange(_categoriesFilter.SelectFromSource(x => x.Id));
	}

	/// <summary>
	/// Keeps <see cref="NavigationColumnWidth" /> narrower than the view, whose new width is <paramref name="viewWidth" />.
	/// </summary>
	[RelayCommand]
	private void SizeChanged(double viewWidth)
	{
		if (NavigationColumnHelper.FitWidth(NavigationColumnWidth, viewWidth) is not { } width)
		{
			return;
		}

		NavigationColumnWidth = width;
	}

	/// <summary>
	/// Switches the container to previous <see cref="FavoriteCategory" />.
	/// </summary>
	[RelayCommand]
	private void SwitchToPrevious() => SelectedCategory = _previousSelectedCategory;
	#endregion

	#region Data
	/// <summary>
	/// <inheritdoc cref="FilteredCollection{T}" />
	/// </summary>
	private readonly FilteredCollection<FavoriteCategory> _categoriesFilter;

	/// <summary>
	/// <inheritdoc cref="FilteredCollection{T}" />
	/// </summary>
	private readonly FilteredCollection<FileDto> _favoritesFilter;

	/// <summary>
	/// Previous <see cref="SelectedCategory" /> value.
	/// </summary>
	private FavoriteCategory? _previousSelectedCategory;

	/// <summary>
	/// Previous <see cref="SelectedFavorite" /> value.
	/// </summary>
	private FileDto? _previousSelectedFavorite;
	#endregion

	#region Constructors
	public SelectedFavoritesViewModel(
		IContentCopier contentCopier,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler,
		TimeProvider timeProvider) : base(
			contentCopier,
			messenger,
			exceptionHandler)
	{
		_categoriesFilter = new(
			this.FilterPredicate(
				x => x.CategorySearch,
				timeProvider),
			autoRefreshOn: x => x.Name);

		_favoritesFilter = new(
			this.FilterPredicate(
				x => x.FavoriteSearch,
				timeProvider),
			autoRefreshOn: x => x.Name);

		// The category and the favorite chosen last come back when the search shows them again while nothing is chosen.
		_categoriesFilter
			.WhenShown(() => _previousSelectedCategory)
			.Where(_ => SelectedCategory is null)
			.Subscribe(x => SelectedCategory = x)
			.DisposeWith(_disposables);

		_favoritesFilter
			.WhenShown(() => _previousSelectedFavorite)
			.Where(_ => SelectedFavorite is null)
			.Subscribe(x => SelectedFavorite = x)
			.DisposeWith(_disposables);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Fills the categories and the favorites from the stored settings.
	/// </summary>
	public void Initialize(
		double navigationColumnWidth,
		Guid selectedCategoryId,
		IReadOnlyCollection<FavoriteCategory> categories,
		IReadOnlyCollection<Guid> orderedCategoryIds,
		IReadOnlyCollection<FavoriteSelection> selectedPairs)
	{
		NavigationColumnWidth = navigationColumnWidth;

		Guid[] identifiers = [.. categories.Select(x => x.Id)];

		// The ids of the categories that are gone stay out of the stored order.
		Guid[] kept = [.. orderedCategoryIds.Where(identifiers.Contains)];

		FavoriteCategory[] ordered = [.. categories.OrderBySequenceKeepSource(kept, x => x.Id)];

		OrderedCategoryIds.AddRange(kept);

		_categoriesFilter.AddRange(ordered);

		SelectedPairs.AddRange(selectedPairs);

		_categoriesFilter.PostToUi(() => SelectedCategory = ordered.FirstOrDefault(x => x.Id == selectedCategoryId));
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		_categoriesFilter.Dispose();

		_favoritesFilter.Dispose();

		_previousSelectedCategory = null;

		_previousSelectedFavorite = null;

		OrderedCategoryIds.Clear();

		SelectedCategory = null;

		SelectedFavorite = null;

		SelectedPairs.Clear();
	}

	/// <inheritdoc />
	protected override void SelectFile(FileDto file) => SelectedFavorite = file;
	#endregion

	#region Helpers
	/// <summary>
	/// Adds or updates value in <see cref="SelectedPairs" />.
	/// </summary>
	private void AddOrUpdateSelectedPairs(Guid favoriteId)
	{
		if (SelectedCategory is not { } category)
		{
			return;
		}

		if (SelectedPairs.FirstOrDefault(x => x.CategoryId == category.Id) is { } pair)
		{
			pair.FavoriteId = favoriteId;
		}
		else
		{
			SelectedPairs.Add(new()
			{
				CategoryId = category.Id,
				FavoriteId = favoriteId
			});
		}
	}
	#endregion
}
