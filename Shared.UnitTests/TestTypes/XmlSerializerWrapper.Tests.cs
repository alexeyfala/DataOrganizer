using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Attributes;
using Shared.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(XmlSerializerWrapper)}"" type")]
internal class XmlSerializerWrapperTests
{
	#region Data
	/// <summary>
	/// XML carrying a document type declaration, which must not be processed.
	/// </summary>
	private const string MaliciousXml = """
		<?xml version="1.0" encoding="utf-8"?>
		<!DOCTYPE XmlSample [
			<!ENTITY payload "exploit">
		]>
		<XmlSample>
			<Name>&payload;</Name>
			<Number>0</Number>
		</XmlSample>
		""";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="XmlSerializerWrapper.Deserialize{T}" />: reads a document that has already been parsed.
	/// </summary>
	[Test]
	public void Deserialize_Reads_A_Document()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		XmlSerializerWrapper sut = mock.Create<XmlSerializerWrapper>();

		XmlSample sample = new()
		{
			Name = "name",
			Number = 42
		};

		XDocument document = XDocument.Parse(sut.Serialize(sample));

		// Act
		XmlSample? result = sut.Deserialize<XmlSample>(document);

		// Assert
		result
			.Should()
			.BeEquivalentTo(sample);
	}

	/// <summary>
	/// <see cref="XmlSerializerWrapper.Deserialize{T}" />: throws when the XML contains a DTD declaration.
	/// </summary>
	[Test]
	[SkipUnderDebugger(Reason = "Asserts a thrown exception; would trigger break-on-throw under debugger.")]
	public void Deserialize_Throws_When_Xml_Contains_DTD_Declaration()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		XmlSerializerWrapper sut = mock.Create<XmlSerializerWrapper>();

		// Act
		Action act = () => sut.Deserialize<XmlSample>(MaliciousXml);

		// Assert
		act
			.Should()
			.Throw<InvalidOperationException>()
			.WithInnerException<XmlException>()
			.WithMessage("*DTD*");
	}

	/// <summary>
	/// <see cref="XmlSerializerWrapper.LoadDocumentAsync" />: reads a stream into a document.
	/// </summary>
	[Test]
	public async Task LoadDocumentAsync_Reads_A_Stream()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		XmlSerializerWrapper sut = mock.Create<XmlSerializerWrapper>();

		XmlSample sample = new()
		{
			Name = "name",
			Number = 42
		};

		await using MemoryStream stream = new();

		sut.Serialize(stream, sample);

		stream.Position = 0;

		// Act
		XDocument result = await sut.LoadDocumentAsync(stream);

		// Assert
		sut
			.Deserialize<XmlSample>(result)
			.Should()
			.BeEquivalentTo(sample);
	}

	/// <summary>
	/// <see cref="XmlSerializerWrapper.LoadDocumentAsync" />: throws when the XML contains a DTD declaration.
	/// </summary>
	[Test]
	[SkipUnderDebugger(Reason = "Asserts a thrown exception; would trigger break-on-throw under debugger.")]
	public async Task LoadDocumentAsync_Throws_When_Xml_Contains_DTD_Declaration()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		XmlSerializerWrapper sut = mock.Create<XmlSerializerWrapper>();

		await using MemoryStream stream = new(Encoding.UTF8.GetBytes(MaliciousXml));

		// Act
		Func<Task> act = () => sut.LoadDocumentAsync(stream);

		// Assert
		await act
			.Should()
			.ThrowAsync<XmlException>()
			.WithMessage("*DTD*");
	}
	#endregion
}

/// <summary>
/// Sample DTO for serialization round-trip tests. Must be a public top-level type so
/// <see cref="XmlSerializer" /> can reflect on it.
/// </summary>
public sealed class XmlSample
{
	public string Name { get; set; } = string.Empty;

	public int Number { get; set; }
}
