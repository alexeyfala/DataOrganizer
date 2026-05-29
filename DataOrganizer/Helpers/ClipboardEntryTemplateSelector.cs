using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Clipboard;

namespace DataOrganizer.Helpers;

/// <summary>
/// Picks the content template for a clipboard entry by its runtime type.
/// </summary>
internal sealed class ClipboardEntryTemplateSelector : TemplateSelectorBase, IDataTemplate
{
	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		if (param is null)
		{
			return null;
		}

		DataTemplate? bestMatch = null;

		foreach (IDataTemplate candidate in DataTemplates)
		{
			if (candidate is not DataTemplate { DataType: { } dataType } template || !dataType.IsInstanceOfType(param))
			{
				continue;
			}

			// Prefer the most-derived matching DataType.
			if (bestMatch?.DataType is null || bestMatch
				.DataType
				.IsAssignableFrom(dataType))
			{
				bestMatch = template;
			}
		}

		return bestMatch?.Build(param);
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is ClipboardHistoryEntryBase;
	#endregion
}
