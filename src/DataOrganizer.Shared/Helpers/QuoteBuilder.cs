using DataOrganizer.Shared.Properties;
using System.Globalization;

namespace DataOrganizer.Shared.Helpers;

/// <summary>
/// Puts a text in the quotes of the language of the interface.
/// </summary>
public static class QuoteBuilder
{
	#region Methods
	/// <summary>
	/// Returns <paramref name="text" /> in the quotes of the language of the interface.
	/// </summary>
	public static string Build(string? text)
	{
		return string.Format(
			CultureInfo.CurrentCulture,
			Strings.QuotedFormat,
			text);
	}
	#endregion
}
