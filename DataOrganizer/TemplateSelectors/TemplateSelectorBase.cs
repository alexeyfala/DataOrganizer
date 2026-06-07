using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using System.Collections.ObjectModel;

namespace DataOrganizer.TemplateSelectors;

internal abstract class TemplateSelectorBase
{
	#region Properties
	/// <summary>
	/// Available data templates.
	/// </summary>
	[Content]
	public Collection<IDataTemplate> DataTemplates { get; } = [];
	#endregion
}
