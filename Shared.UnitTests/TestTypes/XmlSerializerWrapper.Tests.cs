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
	/// Test of <see cref="XmlSerializerWrapper.Deserialize{T}" />.
	/// </summary>
	[Test]
	public void Deserialize_Returns_Object_From_Xml_String()
	{
		// Arrange
		XmlSerializerWrapper sut = new();

		const string xml = """
			<?xml version="1.0" encoding="utf-16"?>
			<XmlSample>
				<Name>beta</Name>
				<Number>7</Number>
			</XmlSample>
			""";

		// Act
		XmlSample? result = sut.Deserialize<XmlSample>(xml);

		// Assert
		result
			.Should()
			.NotBeNull();

		result.Name
			.Should()
			.Be("beta");

		result.Number
			.Should()
			.Be(7);
	}

	/// <summary>
	/// Test of <see cref="XmlSerializerWrapper.Deserialize{T}" />.
	/// </summary>
	[Test]
	[SkipUnderDebugger(Reason = "Asserts a thrown exception; would trigger break-on-throw under debugger.")]
	public void Deserialize_Throws_When_Xml_Contains_DTD_Declaration()
	{
		// Arrange
		XmlSerializerWrapper sut = new();

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

	/// <summary>
	/// Test of <see cref="XmlSerializerWrapper.Serialize{T}" />.
	/// </summary>
	[Test]
	public void Serialize_Returns_Xml_String_From_Object()
	{
		// Arrange
		XmlSerializerWrapper sut = new();

		XmlSample value = new() { Name = "alpha", Number = 42 };

		// Act
		string result = sut.Serialize(value);

		// Assert
		result
			.Should()
			.Contain("<Name>alpha</Name>");

		result
			.Should()
			.Contain("<Number>42</Number>");
	}

	/// <summary>
	/// Test of <see cref="XmlSerializerWrapper.Serialize{T}" /> + <see cref="XmlSerializerWrapper.Deserialize{T}" />.
	/// </summary>
	[Test]
	public void Serialize_Then_Deserialize_Returns_Equivalent_Object()
	{
		// Arrange
		XmlSerializerWrapper sut = new();

		XmlSample value = new() { Name = "gamma", Number = 100 };

		// Act
		string xml = sut.Serialize(value);

		XmlSample? roundTrip = sut.Deserialize<XmlSample>(xml);

		// Assert
		roundTrip
			.Should()
			.BeEquivalentTo(value);
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
