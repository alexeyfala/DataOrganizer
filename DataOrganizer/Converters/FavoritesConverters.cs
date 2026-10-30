using Avalonia.Data.Converters;
using Favorites.Dto;
using Hierarchy.Dto;
using System.Linq;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters of the categories of the favorites, referenced via x:Static.
/// </summary>
internal static class FavoritesConverters
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the folder of a favorites category has a note.
	/// </summary>
	public static FuncValueConverter<FavoriteCategory?, bool> FavoriteCategoryHasNote { get; } =
		new(category => GetFolder(category)?.Note is not null);

	/// <summary>
	/// The folder a favorites category is built from; <c>null</c> for the category of the root objects.
	/// </summary>
	public static FuncValueConverter<FavoriteCategory?, FolderDto?> FavoriteCategoryToFolder { get; } =
		new(GetFolder);
	#endregion

	#region Helpers
	/// <summary>
	/// The parent folder of the objects of a favorites category; a category always has children.
	/// </summary>
	private static FolderDto? GetFolder(FavoriteCategory? category)
	{
		return category
			?.Children
			.FirstOrDefault()
			?.Parent;
	}
	#endregion
}
