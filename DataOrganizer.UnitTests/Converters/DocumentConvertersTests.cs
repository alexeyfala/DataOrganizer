using AwesomeAssertions;
using DataOrganizer.Converters;
using DataOrganizer.Services.Documents;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentConverters)}"" type")]
internal class DocumentConvertersTests
{
	#region Methods
	/// <summary>
	/// <see cref="DocumentConverters.ToTextDocument" />: a text gives the document of the editor that keeps it, and no text
	/// gives none.
	/// </summary>
	[Test]
	public void ToTextDocument_Gives_The_Document_That_Keeps_The_Text()
	{
		// Arrange
		AvaloniaLogText text = new();

		// Act
		object? document = DocumentConverters.ToTextDocument.Convert(
			text,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);

		object? none = DocumentConverters.ToTextDocument.Convert(
			null,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);

		// Assert
		document
			.Should()
			.BeSameAs(text.Document);

		none
			.Should()
			.BeNull();
	}
	#endregion
}
