using DataOrganizer.Dto.Favorites;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// The settings for "Favorites" view.
/// </summary>
public sealed class FavoritesViewSettings
{
	#region Properties
	/// <summary>
	/// A sequence of <see cref="FavoriteCategory" />.
	/// </summary>
	[JsonIgnore]
	public List<FavoriteCategory> Categories { get; } = [];

	/// <summary>
	/// Navigation column width.
	/// </summary>
	public double NavigationColumnWidth { get; set; }

	/// <summary>
	/// Ordered identifiers of categories.
	/// </summary>
	public List<Guid> OrderedCategories { get; set; } = [];

	/// <summary>
	/// Selected category identifier.
	/// </summary>
	public Guid SelectedCategoryId { get; set; }

	/// <summary>
	/// A sequence of <see cref="FavoriteSelection" />.
	/// </summary>
	public List<FavoriteSelection> SelectedPairs { get; set; } = [];
	#endregion
}
