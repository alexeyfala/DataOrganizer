using Cysharp.Text;
using System;
using System.Text.RegularExpressions;

namespace Shared.Extensions;

public static partial class StringExtensions
{
	#region Methods
	/// <summary>
	/// Determines whether a string is a valid Html color format.
	/// </summary>
	public static bool IsHtmlColorFormat(this string? value) => HtmlColorRegex().IsMatch(value ?? string.Empty);

	/// <summary>
	/// Determines whether a string is a valid <see cref="Uri" />.
	/// </summary>
	public static bool IsUriFormat(this string? value) => Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

	/// <summary>
	/// Repeats a string the required number of times.
	/// </summary>
	public static string Repeat(this string value, int count, string separator)
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		for (int i = 0; i < count; i++)
		{
			if (i == count - 1)
			{
				builder.Append(value);
			}
			else
			{
				builder.Append(value);

				builder.Append(separator);
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Wraps the string in quotes if necessary.
	/// </summary>
	/// <remarks>
	/// This method should be used when wrapping paths to file system entries that may contain spaces.
	/// </remarks>
	public static string SurroundWithQuotesIfNeeded(this string value)
	{
		const char quote = '"';

		if (!value.StartsWith(quote))
		{
			value = quote + value;
		}

		if (!value.EndsWith(quote))
		{
			value += quote;
		}

		return value;
	}

	/// <summary>
	/// Performs text trimming.
	/// </summary>
	/// <param name="value">Original string.</param>
	/// <param name="maxLength">Maximum length</param>
	/// <param name="suffix">Placeholder suffix for the trimmed string (usually an ellipsis ...)</param>
	public static string Truncate(
		this string value,
		int maxLength,
		string suffix = "...")
	{
		return value[..Math.Min(value.Length, maxLength)] + (value.Length > maxLength ? suffix : string.Empty);
	}
	#endregion

	#region Helpers
	[GeneratedRegex("^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
	private static partial Regex HtmlColorRegex();
	#endregion
}
