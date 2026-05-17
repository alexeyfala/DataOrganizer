using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
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
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="SelectedFavoritesView" />.
/// </summary>
public sealed partial class SelectedFavoritesViewModel : FileListViewModelBase, INavigationColumnViewModel
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
	/// A sequence of <see cref="FileModelDto" />.
	/// </summary>
	public ReadOnlyObservableCollection<FileModelDto> Favorites => _favoritesFilter.Visible;

	/// <summary>
	/// Search value within <see cref="Favorites" />.
	/// </summary>
	[ObservableProperty]
	public partial string? FavoriteSearch { get; set; }

	/// <summary>
	/// Returns <c>True</c> if <see cref="Categories" /> is empty.
	/// </summary>
	public bool IsCategoriesEmpty => _categoriesFilter.IsSourceEmpty;

	/// <summary>
	/// Returns <c>True</c> if <see cref="Favorites" /> is empty.
	/// </summary>
	public bool IsFavoritesEmpty => _favoritesFilter.IsSourceEmpty;

	/// <inheritdoc cref="INavigationColumnViewModel.NavigationColumnWidth" />
	[ObservableProperty]
	public partial GridLength NavigationColumnWidth { get; set; }

	/// <inheritdoc cref="FavoritesViewSettings.OrderedCategories" />
	public List<Guid> OrderedCategories { get; } = [];

	/// <summary>
	/// The selected object in the <see cref="Categories" />.
	/// </summary>
	[ObservableProperty]
	public partial FavoriteCategory? SelectedCategory { get; set; }

	/// <summary>
	/// The selected object in the <see cref="Favorites" />.
	/// </summary>
	[ObservableProperty]
	public partial FileModelDto? SelectedFavorite { get; set; }

	/// <inheritdoc cref="FavoritesViewSettings.SelectedPairs" />
	public List<CategoryFavoritePair> SelectedPairs { get; } = [];
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
	partial void OnSelectedFavoriteChanged(FileModelDto? oldValue, FileModelDto? newValue)
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
	private void CategoryDragged(DraggedIndexTargetIndexPair pair)
	{
		FavoriteCategory selected = Categories[pair.DraggedIndex];

		_categoriesFilter.Reorder(selected, pair.TargetIndex);

		_categoriesFilter.PostToUi(() => SelectedCategory = selected);

		OrderedCategories.ClearAddRange(_categoriesFilter.SelectFromSource(x => x.Id));
	}

	/// <summary>
	/// <see cref="Control.SizeChanged" /> event handler of <see cref="UserControl" />.
	/// </summary>
	[RelayCommand]
	private void SizeChanged(SizeChangedEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		((INavigationColumnViewModel)this).SetNavigationColumnWidth(e.NewSize.Width);
	}

	/// <summary>
	/// Switches the container to previous <see cref="FavoriteCategory" />.
	/// </summary>
	[RelayCommand]
	private void SwitchToPrevious() => SelectedCategory = _previousSelectedCategory;
	#endregion

	#region Data
	/// <summary>
	/// <inheritdoc cref="FilterEngine{T}" />
	/// </summary>
	private readonly FilterEngine<FavoriteCategory> _categoriesFilter;

	/// <summary>
	/// <inheritdoc cref="FilterEngine{T}" />
	/// </summary>
	private readonly FilterEngine<FileModelDto> _favoritesFilter;

	/// <summary>
	/// Previous <see cref="SelectedCategory" /> value.
	/// </summary>
	private FavoriteCategory? _previousSelectedCategory;

	/// <summary>
	/// Previous <see cref="SelectedFavorite" /> value.
	/// </summary>
	private FileModelDto? _previousSelectedFavorite;
	#endregion

	#region Constructors
	public SelectedFavoritesViewModel(
		Application app,
		IClipboardService clipboard,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEntityEncryption entityEncryption,
		ILogger logger,
		ITaskExceptionHandler handler,
		IViewModelExecutionService viewModel) : base(
			app,
			clipboard,
			dbAccess,
			dialogService,
			entityEncryption,
			logger,
			handler,
			viewModel)
	{
		_categoriesFilter = new(
			this.FilterPredicate(x => x.CategorySearch),
			autoRefreshOn: x => x.Name);

		_favoritesFilter = new(
			this.FilterPredicate(x => x.FavoriteSearch),
			autoRefreshOn: x => x.Name);

		if (_categoriesFilter.Visible is INotifyCollectionChanged categoriesNotifier)
		{
			categoriesNotifier.CollectionChanged += CategoriesNotifier_CollectionChanged;

			Disposable
				.Create(() => categoriesNotifier.CollectionChanged -= CategoriesNotifier_CollectionChanged)
				.DisposeWith(_disposables);
		}

		if (_favoritesFilter.Visible is INotifyCollectionChanged favoritesNotifier)
		{
			favoritesNotifier.CollectionChanged += FavoritesNotifier_CollectionChanged;

			Disposable
				.Create(() => favoritesNotifier.CollectionChanged -= FavoritesNotifier_CollectionChanged)
				.DisposeWith(_disposables);
		}
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="FilterEngine{TModel}.Visible" />.<see cref="INotifyCollectionChanged.CollectionChanged" /> event handler for categories.
	/// </summary>
	private void CategoriesNotifier_CollectionChanged(
		object? sender,
		NotifyCollectionChangedEventArgs e)
	{
		if (SelectedCategory is not null
			|| _previousSelectedCategory is null
			|| !_categoriesFilter.Visible.Contains(_previousSelectedCategory))
		{
			return;
		}

		SelectedCategory = _previousSelectedCategory;
	}

	/// <summary>
	/// <see cref="FilterEngine{TModel}.Visible" />.<see cref="INotifyCollectionChanged.CollectionChanged" /> event handler for favorites.
	/// </summary>
	private void FavoritesNotifier_CollectionChanged(
		object? sender,
		NotifyCollectionChangedEventArgs e)
	{
		if (SelectedFavorite is not null
			|| _previousSelectedFavorite is null
			|| !_favoritesFilter.Visible.Contains(_previousSelectedFavorite))
		{
			return;
		}

		SelectedFavorite = _previousSelectedFavorite;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Adds <see cref="FavoriteCategory" /> objects to the source.
	/// </summary>
	public void AddTestCategories(IEnumerable<FavoriteCategory> items)
	{
		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			throw new InvalidOperationException("This method created for test purposes only, do not use it directly in code!");
		}

		_categoriesFilter.AddRange(items);
	}

	/// <summary>
	/// Adds <see cref="FileModelDto" /> objects to the source.
	/// </summary>
	public void AddTestFavorites(IEnumerable<FileModelDto> items)
	{
		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			throw new InvalidOperationException("This method created for test purposes only, do not use it directly in code!");
		}

		_favoritesFilter.AddRange(items);
	}

	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(
		double navigationColumnWidth,
		Guid selectedCategoryId,
		List<FavoriteCategory> categories,
		List<Guid> orderedCategories,
		List<CategoryFavoritePair> selectedPairs)
	{
		NavigationColumnWidth = new(navigationColumnWidth);

		if (orderedCategories.Count > 0)
		{
			Guid[] identifiers = [.. categories.Select(x => x.Id)];

			for (int i = 0; i < orderedCategories.Count; i++)
			{
				if (!identifiers.Contains(orderedCategories[i]))
				{
					orderedCategories.RemoveAt(i);
				}
			}

			if (orderedCategories.Count > 0)
			{
				categories.ClearAddRange([.. categories.OrderBySequenceKeepSource(orderedCategories, x => x.Id)]);

				OrderedCategories.AddRange(orderedCategories);
			}
		}

		_categoriesFilter.AddRange(categories);

		SelectedPairs.AddRange(selectedPairs);

		_categoriesFilter.PostToUi(() => SelectedCategory = categories.FirstOrDefault(x => x.Id == selectedCategoryId));
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		_categoriesFilter.Dispose();

		_favoritesFilter.Dispose();

		_previousSelectedCategory = null;

		_previousSelectedFavorite = null;

		OrderedCategories.Clear();

		SelectedCategory = null;

		SelectedFavorite = null;

		SelectedPairs.Clear();
	}
	#endregion

	#region Service
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
