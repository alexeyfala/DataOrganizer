using AwesomeAssertions;
using DataOrganizer.Services.Documents;
using EditorDocuments.Interfaces;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentTextFactory)}"" type")]
internal class DocumentTextFactoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="DocumentTextFactory.Create" />: each call gives an empty text of its own, with the document of the editor.
	/// </summary>
	[Test]
	public void Create_Makes_An_Empty_Text_Each_Time()
	{
		// Arrange
		DocumentTextFactory sut = new();

		// Act
		IDocumentText first = sut.Create();

		IDocumentText second = sut.Create();

		// Assert
		first
			.Should()
			.BeOfType<AvaloniaDocumentText>()
			.Which
			.Text
			.Should()
			.BeEmpty();

		second
			.Should()
			.NotBeSameAs(first);
	}
	#endregion
}
