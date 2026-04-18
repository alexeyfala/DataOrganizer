using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using DataOrganizer.Abstract;
using System.Linq;

namespace DataOrganizer.Helpers;

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

		return ViewLocator.GetPlugControl(typeName);
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is DatasetRecordBase;
	#endregion
}
