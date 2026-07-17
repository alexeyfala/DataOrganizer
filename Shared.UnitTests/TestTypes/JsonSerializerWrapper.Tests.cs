using Autofac.Extras.Moq;
using AwesomeAssertions;
using Shared.Services;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(JsonSerializerWrapper)}"" type")]
internal class JsonSerializerWrapperTests
{
	#region Methods
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
