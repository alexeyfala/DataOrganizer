using AwesomeAssertions;
using Repository.Dto;

namespace Repository.UnitTests.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(PropertyDescription)}"" type")]
internal class PropertyDescriptionTests
{
	#region Methods
	/// <summary>
	/// <see cref="PropertyDescription(string, object?)" />: the name and the value are kept as given.
	/// </summary>
	[Test]
	public void Constructor_Keeps_The_Name_And_The_Value()
	{
		// Arrange
		object value = new();

		// Act
		PropertyDescription sut = new("Size", value);

		// Assert
		sut.Name
			.Should()
			.Be("Size");

		sut.Value
			.Should()
			.BeSameAs(value);
	}
	#endregion
}
