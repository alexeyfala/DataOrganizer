using AwesomeAssertions;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(TextTransformExtensions)}"" type")]
internal class TextTransformExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="TextTransformExtensions.NeedsSelection" />: only the changes of the indentation and the removal of the
	/// trailing spaces take the whole document without a selection.
	/// </summary>
	[TestCase(TextTransform.UpperCase, true)]
	[TestCase(TextTransform.LowerCase, true)]
	[TestCase(TextTransform.TitleCase, true)]
	[TestCase(TextTransform.InvertCase, true)]
	[TestCase(TextTransform.RemoveLeadingWhitespace, true)]
	[TestCase(TextTransform.RemoveTrailingWhitespace, false)]
	[TestCase(TextTransform.LeadingTabsToSpaces, false)]
	[TestCase(TextTransform.LeadingSpacesToTabs, false)]
	public void NeedsSelection_Spares_The_Document_From_A_Stray_Click(TextTransform transform, bool expected)
	{
		// Act
		bool result = transform.NeedsSelection();

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
