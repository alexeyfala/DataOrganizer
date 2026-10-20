using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataOrganizer.Converters;

/// <summary>
/// Automation name of a button that shows no text: its tip, while the tip is text. Inputs: [Content, ToolTip.Tip].
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
		return values is [not string, string { Length: > 0 } tip]
			? tip
			: AvaloniaProperty.UnsetValue;
	}
	#endregion
}
