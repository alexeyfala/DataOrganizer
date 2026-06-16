using DataOrganizer.Helpers;
using Shared.Properties;
using System;
using System.Collections.Generic;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Text entry whose whole trimmed content matches an absolute http(s) URL.
/// </summary>
public sealed class ClipboardUrlEntry : ClipboardTextEntry
{
	#region Properties
	/// <inheritdoc />
	public override string? ContentToolTip => field ??= BuildContentToolTip();

	/// <inheritdoc />
	public override bool IsUrl => true;

	/// <inheritdoc />
	public override string TypeGlyph => Glyphs.Link;

	/// <inheritdoc />
	public override string TypeToolTip => Strings.Hyperlink;

	/// <summary>
	/// Trimmed http(s) URL (whole-string match) used by the "open externally" action.
	/// </summary>
	public required string Url { get; init; }
	#endregion

	#region Data
	/// <summary>
	/// Maximum characters per wrapped line of the URL tooltip.
	/// </summary>
	private const int ContentToolTipMaxLineLength = 64;

	/// <summary>
	/// Maximum number of lines rendered by the URL tooltip.
	/// </summary>
	private const int ContentToolTipMaxLines = 10;
	#endregion

	#region Helpers
	/// <summary>
	/// Wraps <paramref name="url" /> at <see cref="ContentToolTipMaxLineLength" /> chars,
	/// caps it at <see cref="ContentToolTipMaxLines" /> lines and appends "..." when truncated.
	/// </summary>
	private static IEnumerable<string> EnumerateToolTipLines(string url)
	{
		int totalLines = (url.Length + ContentToolTipMaxLineLength - 1) / ContentToolTipMaxLineLength;

		bool truncated = totalLines > ContentToolTipMaxLines;

		int visibleLines = truncated ? ContentToolTipMaxLines - 1 : totalLines;

		for (int i = 0; i < visibleLines; i++)
		{
			int start = i * ContentToolTipMaxLineLength;

			int length = Math.Min(ContentToolTipMaxLineLength, url.Length - start);

			yield return url.Substring(start, length);
		}

		if (truncated)
		{
			yield return "...";
		}
	}

	/// <summary>
	/// Builds the wrapped, length-capped tooltip for <see cref="Url" />.
	/// </summary>
	private string BuildContentToolTip() => string.Join(Environment.NewLine, EnumerateToolTipLines(Url));
	#endregion
}
