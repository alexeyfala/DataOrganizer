using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using System.Collections.ObjectModel;

namespace DataOrganizer.Templates.Selectors;

/// <summary>
/// Base for a template selector, holding the data templates declared for it in markup.
/// </summary>
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
