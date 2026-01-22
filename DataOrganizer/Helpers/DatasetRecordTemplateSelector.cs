using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using DataOrganizer.Abstract;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers;

internal sealed class DatasetRecordTemplateSelector : IDataTemplate
{
	#region Properties
	/// <summary>
	/// Available data templates.
	/// </summary>
	[Content]
	public ICollection<IDataTemplate> DataTemplates { get; } = [];
	#endregion

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
