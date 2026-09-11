using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DataOrganizer.Enums.Views;

namespace DataOrganizer.Templates.Selectors;

/// <summary>
/// Picks the template for the right side sheet by the requested content kind.
/// </summary>
internal sealed class RightSideSheetContentTemplateSelector : TemplateSelectorBase, IDataTemplate
{
	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		return param switch
		{
			RightSideSheetContentKind.CopyHistory => DataTemplates[0].Build(param),
			RightSideSheetContentKind.ExecutingFiles => DataTemplates[1].Build(param),
			_ => new()
		};
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is RightSideSheetContentKind;
	#endregion
}
