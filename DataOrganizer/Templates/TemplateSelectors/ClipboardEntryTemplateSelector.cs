using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using DataOrganizer.DTO.Clipboard;

namespace DataOrganizer.Templates.TemplateSelectors;

/// <summary>
/// Picks the content template for a clipboard entry by its runtime type.
/// </summary>
internal sealed class ClipboardEntryTemplateSelector : TemplateSelectorBase, IDataTemplate
{
	#region Properties
	/// <summary>
	/// Template for <see cref="ClipboardTextEntry" /> with a renderable
	/// <see cref="ClipboardTextEntry.FormattedTextPreview" /> (HTML / RTF / HTML + RTF).
	/// </summary>
	public IDataTemplate? FormattedTextTemplate { get; set; }

	/// <summary>
	/// Template for plain-text <see cref="ClipboardTextEntry" /> (no formatting).
	/// </summary>
	public IDataTemplate? PlainTextTemplate { get; set; }
	#endregion

	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		if (param is null)
		{
			return null;
		}

		if (param is ClipboardTextEntry textEntry)
		{
			IDataTemplate? textTemplate = textEntry.IsFormattedText
				? FormattedTextTemplate
				: PlainTextTemplate;

			return textTemplate?.Build(param);
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
