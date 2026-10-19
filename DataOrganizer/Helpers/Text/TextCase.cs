using System.Globalization;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Changes of the letter case of a text that the editor engine does not make.
/// </summary>
internal static class TextCase
{
	#region Methods
	/// <summary>
	/// Returns the text with every word starting with a capital letter and the rest of its letters in lower case.
	/// </summary>
	public static string ToTitleCase(string text, CultureInfo culture)
	{
		TextInfo textInfo = culture.TextInfo;

		// A word in capitals is taken for an abbreviation and kept, so the text goes to lower case first.
		return textInfo.ToTitleCase(textInfo.ToLower(text));
	}
	#endregion
}
