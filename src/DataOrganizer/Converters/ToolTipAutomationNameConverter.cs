using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataOrganizer.Converters;

/// <summary>
/// Automation name of a button that shows no text, only an icon or a glyph: its tip, while the tip is text or a text
/// block. Inputs: [Content, ToolTip.Tip].
/// </summary>
internal sealed class ToolTipAutomationNameConverter : IMultiValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		IList<object?> values,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		// Text content names the button by itself; an unset value keeps a name set with a lower priority.
		return values is [var content, var tip] && GetText(tip) is { Length: > 0 } text && !IsText(content)
			? text
			: AvaloniaProperty.UnsetValue;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the text of a tip, which is the tip itself or the text of a text block; <c>null</c> for any other tip.
	/// </summary>
	private static string? GetText(object? tip) => tip switch
	{
		string text => text,
		TextBlock block => block.Text,
		_ => null
	};

	/// <summary>
	/// Whether the content reads as text: a string with a letter or a digit, unlike a glyph such as an emoji.
	/// </summary>
	private static bool IsText(object? content) => content is string text && text.Any(char.IsLetterOrDigit);
	#endregion
}
