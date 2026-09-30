using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(SampleDocuments)}"" type")]
internal class SampleDocumentsTests
{
	#region Methods
	/// <summary>
	/// <see cref="SampleDocuments.CreateJson" />: the text is a well-formed JSON object.
	/// </summary>
	[Test]
	public void CreateJson_Writes_A_Well_Formed_Object()
	{
		// Act
		string text = SampleDocuments.CreateJson(SampleFaker.Create());

		// Assert
		JsonNode.Parse(text)
			.Should()
			.BeOfType<JsonObject>();
	}

	/// <summary>
	/// <see cref="SampleDocuments.CreateXml" />: the text is a well-formed XML document that keeps its declaration.
	/// </summary>
	[Test]
	public void CreateXml_Writes_A_Well_Formed_Document()
	{
		// Act
		string text = SampleDocuments.CreateXml(SampleFaker.Create());

		// Assert
		XDocument.Parse(text).Declaration
			.Should()
			.NotBeNull();
	}
	#endregion
}
