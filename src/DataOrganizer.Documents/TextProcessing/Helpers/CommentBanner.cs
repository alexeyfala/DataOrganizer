using DataOrganizer.Shared.Constants;
using System;

namespace DataOrganizer.Documents.TextProcessing.Helpers;

/// <summary>
/// Banner of a folded comment: its first line with dots after it, as Visual Studio shows it in the box.
/// </summary>
public static class CommentBanner
{
	#region Methods
	/// <summary>
	/// Returns the banner of a comment from the text of its first line, from its token on.
	/// </summary>
	public static string Create(ReadOnlySpan<char> firstLine) => $"{firstLine.TrimEnd()} {Glyphs.ThreeDots}";
	#endregion
}
