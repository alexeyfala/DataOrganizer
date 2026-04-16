using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DataOrganizer.Abstract;
using DataOrganizer.Enums;

namespace DataOrganizer.Helpers;

internal sealed class RightSideSheetContentTemplateSelector : TemplateSelectorBase, IDataTemplate
{
	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		return param switch
		{
			EditorRightSideSheetContentType.CopyHistory => DataTemplates[0].Build(param),
			EditorRightSideSheetContentType.ExecutedFiles => DataTemplates[1].Build(param),
			_ => new()
		};
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is EditorRightSideSheetContentType;
	#endregion
}
