using AwesomeAssertions;
using Shared.Services;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(JsonSerializerWrapper)}"" type")]
internal class JsonSerializerWrapperTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="JsonSerializerWrapper.Deserialize{T}" />.
	/// </summary>
	[Test]
	public void Deserialize_Returns_Object_From_Json_String()
	{
		// Arrange
		JsonSerializerWrapper sut = new();

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
	/// Test of <see cref="JsonSerializerWrapper.Serialize{T}" />.
	/// </summary>
	[Test]
	public void Serialize_Returns_Json_String_From_Object()
	{
		// Arrange
		JsonSerializerWrapper sut = new();

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
	/// Test of <see cref="JsonSerializerWrapper.Serialize{T}" /> + <see cref="JsonSerializerWrapper.Deserialize{T}" />.
	/// </summary>
	[Test]
	public void Serialize_Then_Deserialize_Returns_Equivalent_Object()
	{
		// Arrange
		JsonSerializerWrapper sut = new();

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
	/// Test of <see cref="JsonSerializerWrapper.ToReadableJson{T}" />.
	/// </summary>
	[Test]
	public void ToReadableJson_Includes_Type_Name_And_Json_Body_For_Non_Null_Value()
	{
		// Arrange
		JsonSerializerWrapper sut = new();

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
	/// Test of <see cref="JsonSerializerWrapper.ToReadableJson{T}" />.
	/// </summary>
	[Test]
	public void ToReadableJson_Returns_Null_Marker_For_Null_Reference()
	{
		// Arrange
		JsonSerializerWrapper sut = new();

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

	#region Service
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
