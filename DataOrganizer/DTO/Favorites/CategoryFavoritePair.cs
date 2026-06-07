using System;
using System.Diagnostics;

namespace DataOrganizer.DTO.Favorites;

/// <summary>
/// A combination of the selected category and the child object selected within it.
/// </summary>
[DebuggerDisplay($"{nameof(CategoryId)} = {{{nameof(CategoryId)}}}, {nameof(FavoriteId)} = {{{nameof(FavoriteId)}}}")]
public class CategoryFavoritePair
{
	#region Properties
	/// <summary>
	/// Category identifier.
	/// </summary>
	public required Guid CategoryId { get; init; }

	/// <summary>
	/// Favorite identifier.
	/// </summary>
	public required Guid FavoriteId { get; set; }
	#endregion Properties
}
