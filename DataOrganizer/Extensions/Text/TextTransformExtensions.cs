using DataOrganizer.Enums.Documents;

namespace DataOrganizer.Extensions.Text;

internal static class TextTransformExtensions
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
