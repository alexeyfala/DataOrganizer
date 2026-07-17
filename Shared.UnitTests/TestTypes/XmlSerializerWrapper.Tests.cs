using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Attributes;
using Shared.Services;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(XmlSerializerWrapper)}"" type")]
internal class XmlSerializerWrapperTests
{
	#region Methods
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

		const string maliciousXml = """
			<?xml version="1.0" encoding="utf-8"?>
			<!DOCTYPE XmlSample [
				<!ENTITY payload "exploit">
			]>
			<XmlSample>
				<Name>&payload;</Name>
				<Number>0</Number>
			</XmlSample>
			""";

		// Act
		Action act = () => sut.Deserialize<XmlSample>(maliciousXml);

		// Assert
		act
			.Should()
			.Throw<InvalidOperationException>()
			.WithInnerException<XmlException>()
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
