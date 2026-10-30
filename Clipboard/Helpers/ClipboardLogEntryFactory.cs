using Clipboard.Models;
using DataProtection.Helpers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Clipboard.Helpers;

/// <summary>
/// Creates the entries of the clipboard log, a text that is a whole link as a link entry and a text that looks like a
/// secret as a sensitive one.
/// </summary>
public static partial class ClipboardLogEntryFactory
{
	#region Methods
	/// <summary>
	/// Creates the entry of a list of files and folders.
	/// </summary>
	public static ClipboardFilesEntry CreateFiles(
		IReadOnlyList<ClipboardFileSystemEntry> fileSystemEntries,
		byte[] hash,
		bool isPinned = false)
	{
		return new()
		{
			FileSystemEntries = fileSystemEntries,
			Hash = hash,
			IsPinned = isPinned
		};
	}

	/// <summary>
	/// Creates the entry of an image.
	/// </summary>
	public static ClipboardImageEntry CreateImage(
		byte[] png,
		byte[] hash,
		bool isPinned = false)
	{
		return new()
		{
			Hash = hash,
			IsPinned = isPinned,
			OriginalPng = png
		};
	}

	/// <summary>
	/// Creates the entry of a text: a link when the whole trimmed text is an http(s) URL, plain text otherwise;
	/// sensitive when the text looks like a secret.
	/// </summary>
	public static ClipboardTextEntryBase CreateText(
		string text,
		string? html,
		string? rtf,
		byte[] hash,
		bool isPinned = false)
	{
		string trimmed = text.Trim();

		bool isSensitive = SensitiveTextDetector.LooksLikeSecret(text);

		if (!WholeStringUrlRegex().IsMatch(trimmed))
		{
			return new ClipboardTextEntry
			{
				Hash = hash,
				Html = html,
				IsPinned = isPinned,
				IsSensitive = isSensitive,
				Rtf = rtf,
				Text = text
			};
		}

		return new ClipboardUrlEntry
		{
			Hash = hash,
			Html = html,
			IsPinned = isPinned,
			IsSensitive = isSensitive,
			Rtf = rtf,
			Text = text,
			Url = trimmed
		};
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches when the entire input is an http(s) URL.
	/// </summary>
	[GeneratedRegex(@"^https?://\S+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	private static partial Regex WholeStringUrlRegex();
	#endregion
}
