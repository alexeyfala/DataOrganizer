using DataOrganizer.Enums.Documents;
using Shared.Properties;

namespace DataOrganizer.Extensions;

internal static class LineEndingExtensions
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
	#endregion
}
