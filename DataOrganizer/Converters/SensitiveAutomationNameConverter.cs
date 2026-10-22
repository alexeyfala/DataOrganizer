using Avalonia;
using Avalonia.Data.Converters;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataOrganizer.Converters;

/// <summary>
/// Automation name of a text that may be sensitive: the text, or a neutral word while it is sensitive, so that none of it
/// reaches UI Automation. Inputs: [Text, IsSensitive].
/// </summary>
internal sealed class SensitiveAutomationNameConverter : IMultiValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		IList<object?> values,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		if (values is [_, true])
		{
			return Strings.HiddenText;
		}

		// An unset value keeps a name set with a lower priority.
		return values is [string text, _]
			? text
			: AvaloniaProperty.UnsetValue;
	}
	#endregion
}
