using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataOrganizer.Converters;

/// <summary>
/// Automation name of an input field: its label, or else its placeholder, while it is text. Inputs: [Label, PlaceholderText].
/// </summary>
internal sealed class FieldLabelAutomationNameConverter : IMultiValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		IList<object?> values,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		// An unset value keeps a name set with a lower priority.
		return values
			.OfType<string>()
			.FirstOrDefault(static x => x.Length > 0) ?? AvaloniaProperty.UnsetValue;
	}
	#endregion
}
