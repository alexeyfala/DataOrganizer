using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using DataOrganizer.Helpers;
using DataOrganizer.Models.Dataset;
using System.Linq;

namespace DataOrganizer.TemplateSelectors;

internal sealed class DatasetRecordTemplateSelector : TemplateSelectorBase, IDataTemplate
{
	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		string? typeName = param?
			.GetType()
			.Name;

		if (typeName is not null
			&& DataTemplates.FirstOrDefault(x => x is DataTemplate y && y.DataType?.Name == typeName) is { } template)
		{
			return template.Build(param);
		}

		return PlugControl.Create(typeName);
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is DatasetRecordBase;
	#endregion
}
