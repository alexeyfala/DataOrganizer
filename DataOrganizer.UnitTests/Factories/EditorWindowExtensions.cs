using Avalonia.Controls;
using Avalonia.VisualTree;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Windows;
using System.Linq;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Access to the rows of the tree of an editor window by the objects they show.
/// </summary>
internal static class EditorWindowExtensions
{
	#region Methods
	/// <summary>
	/// Returns the item of the tree that shows an object; <c>null</c> while the tree shows none.
	/// </summary>
	public static TreeViewItem? FindTreeItem(this EditorWindow window, ExplorerItemDtoBase dto)
	{
		return window
			.FindTreeRow(dto)?
			.FindAncestorOfType<TreeViewItem>();
	}

	/// <summary>
	/// Returns the content of the row of the tree that shows an object; <c>null</c> while the tree shows none.
	/// </summary>
	public static Grid? FindTreeRow(this EditorWindow window, ExplorerItemDtoBase dto)
	{
		return window
			.GetVisualDescendants()
			.OfType<Grid>()
			.FirstOrDefault(x => x.DataContext == dto && x.ContextFlyout is LazyMenuFlyout);
	}
	#endregion
}
