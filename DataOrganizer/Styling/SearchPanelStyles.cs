using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaEdit.Search;
using Shared.Properties;

namespace DataOrganizer.Styling;

/// <summary>
/// Styles of the search panel of the text editors: a tip in the language of the app for its close button, which the
/// theme of the package leaves with a glyph alone.
/// </summary>
internal sealed class SearchPanelStyles : Styles
{
	#region Constructors
	/// <summary>
	/// Builds the styles in code, as markup cannot pick a button of the template by its command.
	/// </summary>
	public SearchPanelStyles()
	{
		Add(new Style(static x => x
			.OfType<SearchPanel>()
			.Template()
			.OfType<Button>()
			.PropertyEquals(Button.CommandProperty, SearchCommands.CloseSearchPanel))
		{
			Setters =
			{
				new Setter(ToolTip.TipProperty, Strings.Close)
			}
		});
	}
	#endregion
}
