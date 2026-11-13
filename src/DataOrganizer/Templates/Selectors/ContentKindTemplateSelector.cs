using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataOrganizer.Templates.Selectors;

/// <summary>
/// Picks the template for a content by its kind.
/// </summary>
internal sealed class ContentKindTemplateSelector : IDataTemplate
{
	#region Properties
	/// <summary>
	/// Templates of the content kinds.
	/// </summary>
	[Content]
	public Collection<ContentKindTemplate> Templates { get; } = [];
	#endregion

	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		if (Templates.FirstOrDefault(x => Equals(x.Kind, param))?.Template is not { } template)
		{
			return new();
		}

		return template.Build(param);
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is not null && Templates.Any(x => x.Kind?.GetType() == data.GetType());
	#endregion
}
