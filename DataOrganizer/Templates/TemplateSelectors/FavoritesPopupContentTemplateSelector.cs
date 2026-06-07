using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DataOrganizer.Enums;

namespace DataOrganizer.Templates.TemplateSelectors;

internal sealed class FavoritesPopupContentTemplateSelector : TemplateSelectorBase, IDataTemplate
{
	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		return param switch
		{
			FavoritesPopupContentType.CopyHistory => DataTemplates[0].Build(param),
			FavoritesPopupContentType.Favorites => DataTemplates[1].Build(param),
			_ => new()
		};
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is FavoritesPopupContentType;
	#endregion
}
