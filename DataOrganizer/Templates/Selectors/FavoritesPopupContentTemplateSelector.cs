using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DataOrganizer.Enums;

namespace DataOrganizer.Templates.Selectors;

/// <summary>
/// Picks the template for the favorites popup by the requested content kind.
/// </summary>
internal sealed class FavoritesPopupContentTemplateSelector : TemplateSelectorBase, IDataTemplate
{
	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		return param switch
		{
			FavoritesPopupContentKind.CopyHistory => DataTemplates[0].Build(param),
			FavoritesPopupContentKind.Favorites => DataTemplates[1].Build(param),
			_ => new()
		};
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is FavoritesPopupContentKind;
	#endregion
}
