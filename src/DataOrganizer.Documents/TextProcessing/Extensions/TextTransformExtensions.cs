using DataOrganizer.Documents.TextProcessing.Enums;

namespace DataOrganizer.Documents.TextProcessing.Extensions;

/// <summary>
/// Tells which transformations of a text work on a selection only.
/// </summary>
public static class TextTransformExtensions
{
	#region Methods
	/// <summary>
	/// <c>True</c> for a transformation that works on a selection only: without one the engine takes the whole
	/// document, which a stray click must not rewrite.
	/// </summary>
	public static bool NeedsSelection(this TextTransform transform)
	{
		return transform is not (TextTransform.LeadingSpacesToTabs
			or TextTransform.LeadingTabsToSpaces
			or TextTransform.RemoveTrailingWhitespace);
	}
	#endregion
}
