using EditorDocuments.Enums;
using Shared.Properties;
using System;

namespace EditorDocuments.Extensions;

public static class LineEndingExtensions
{
	#region Methods
	/// <summary>
	/// Returns the caption of a line break style, named after its system and characters; <c>null</c> for a document
	/// without line breaks.
	/// </summary>
	public static string? ToCaption(this LineEnding lineEnding) => lineEnding switch
	{
		LineEnding.CrLf => "Windows (CR LF)",
		LineEnding.Lf => "Unix (LF)",
		LineEnding.Cr => "Macintosh (CR)",
		LineEnding.Mixed => Strings.MixedLineEndings,
		_ => null
	};

	/// <summary>
	/// Returns the characters of a line break style.
	/// </summary>
	public static string ToNewLine(this LineEnding lineEnding) => lineEnding switch
	{
		LineEnding.Cr => "\r",
		LineEnding.CrLf => "\r\n",
		LineEnding.Lf => "\n",
		_ => throw new NotImplementedException()
	};
	#endregion
}
