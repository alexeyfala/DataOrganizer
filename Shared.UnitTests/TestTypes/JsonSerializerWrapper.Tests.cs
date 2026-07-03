using Autofac.Extras.Moq;
using AwesomeAssertions;
using Shared.Services;
using System.Text;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(JsonSerializerWrapper)}"" type")]
internal class JsonSerializerWrapperTests
{
	#region Methods
	/// <summary>
	/// <see cref="JsonSerializerWrapper.Deserialize{T}(byte[])" />: parses UTF-8 JSON bytes into the expected object.
	/// </summary>
	[Test]
	public void Deserialize_From_Utf8_Bytes_Returns_Object()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		JsonSerializerWrapper sut = mock.Create<JsonSerializerWrapper>();

		byte[] utf8Json = Encoding.UTF8.GetBytes("""{"Name":"delta","Number":13}""");

		// Act
		Sample? result = sut.Deserialize<Sample>(utf8Json);

		// Assert
		result
			.Should()
			.NotBeNull();

		result.Name
			.Should()
			.Be("delta");

		result.Number
			.Should()
			.Be(13);
	}

	/// <summary>
	/// <see cref="JsonSerializerWrapper.Deserialize{T}" />: parses a JSON string into the expected object.
	/// </summary>
	[Test]
	public void Deserialize_Returns_Object_From_Json_String()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		JsonSerializerWrapper sut = mock.Create<JsonSerializerWrapper>();

		const string json = """{"Name":"alpha","Number":42}""";

		// Act
		Sample? result = sut.Deserialize<Sample>(json);

		// Assert
		result
			.Should()
			.NotBeNull();

		result.Name
			.Should()
			.Be("alpha");

		result.Number
			.Should()
			.Be(42);
	}

	/// <summary>
	/// <see cref="JsonSerializerWrapper.Serialize{T}" />: produces a JSON string containing the object's properties.
	/// </summary>
	[Test]
	public void Serialize_Returns_Json_String_From_Object()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		JsonSerializerWrapper sut = mock.Create<JsonSerializerWrapper>();

		Sample value = new() { Name = "beta", Number = 7 };

		// Act
		string result = sut.Serialize(value);

		// Assert
		result
			.Should()
			.Contain("\"Name\":\"beta\"");

		result
			.Should()
			.Contain("\"Number\":7");
	}

	/// <summary>
	/// <see cref="JsonSerializerWrapper.Serialize{T}" /> + <see cref="JsonSerializerWrapper.Deserialize{T}" />: a serialize/deserialize round-trip yields an equivalent object.
	/// </summary>
	[Test]
	public void Serialize_Then_Deserialize_Returns_Equivalent_Object()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		JsonSerializerWrapper sut = mock.Create<JsonSerializerWrapper>();

		Sample value = new() { Name = "gamma", Number = 100 };

		// Act
		string json = sut.Serialize(value);

		Sample? roundTrip = sut.Deserialize<Sample>(json);

		// Assert
		roundTrip
			.Should()
			.BeEquivalentTo(value);
	}

	/// <summary>
	/// <see cref="JsonSerializerWrapper.SerializeToUtf8Bytes{T}" />: produces the same UTF-8 bytes as
	/// encoding the string-based serialization, guaranteeing parity with already-stored content.
	/// </summary>
	[Test]
	public void SerializeToUtf8Bytes_Matches_Encoded_String_Serialization()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		JsonSerializerWrapper sut = mock.Create<JsonSerializerWrapper>();

		Sample value = new() { Name = "delta", Number = 13 };

		// Act
		byte[] fromBytes = sut.SerializeToUtf8Bytes(value);

		byte[] fromString = Encoding.UTF8.GetBytes(sut.Serialize(value));

		// Assert
		fromBytes
			.Should()
			.Equal(fromString);
	}

	/// <summary>
	/// <see cref="JsonSerializerWrapper.SerializeToUtf8Bytes{T}" /> + <see cref="JsonSerializerWrapper.Deserialize{T}(byte[])" />:
	/// a byte round-trip yields an equivalent object.
	/// </summary>
	[Test]
	public void SerializeToUtf8Bytes_Then_Deserialize_From_Bytes_Returns_Equivalent_Object()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		JsonSerializerWrapper sut = mock.Create<JsonSerializerWrapper>();

		Sample value = new() { Name = "zeta", Number = 256 };

		// Act
		byte[] utf8Json = sut.SerializeToUtf8Bytes(value);

		Sample? roundTrip = sut.Deserialize<Sample>(utf8Json);

		// Assert
		roundTrip
			.Should()
			.BeEquivalentTo(value);
	}

	/// <summary>
	/// <see cref="JsonSerializerWrapper.ToReadableJson{T}" />: includes the type name and JSON body for a non-null value.
	/// </summary>
	[Test]
	public void ToReadableJson_Includes_Type_Name_And_Json_Body_For_Non_Null_Value()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		JsonSerializerWrapper sut = mock.Create<JsonSerializerWrapper>();

		Sample value = new() { Name = "epsilon", Number = 5 };

		// Act
		string result = sut.ToReadableJson(value);

		// Assert
		result
			.Should()
			.Contain(typeof(Sample).FullName!);

		result
			.Should()
			.Contain("epsilon");

		result
			.Should()
			.Contain("to Json");
	}

	/// <summary>
	/// <see cref="JsonSerializerWrapper.ToReadableJson{T}" />: includes the type name and a null marker for a null reference.
	/// </summary>
	[Test]
	public void ToReadableJson_Returns_Null_Marker_For_Null_Reference()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		JsonSerializerWrapper sut = mock.Create<JsonSerializerWrapper>();

		// Act
		string result = sut.ToReadableJson<Sample>(null);

		// Assert
		result
			.Should()
			.Contain("= null");

		result
			.Should()
			.Contain(typeof(Sample).FullName!);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Sample DTO for serialization round-trip tests.
	/// </summary>
	private sealed class Sample
	{
		public string Name { get; init; } = string.Empty;

		public int Number { get; init; }
	}
	#endregion
}
