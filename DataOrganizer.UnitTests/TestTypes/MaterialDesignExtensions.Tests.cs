using Avalonia.Media;
using AwesomeAssertions;
using DataOrganizer.Extensions;
using Material.Colors;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(MaterialDesignExtensions)}"" type")]
internal class MaterialDesignExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="MaterialDesignExtensions.GetBrush(PrimaryColor)" />.
	/// </summary>
	[Test]
	public void GetBrush_For_PrimaryColor_Returns_SolidColorBrush_With_Mapped_Color()
	{
		// Arrange
		const PrimaryColor primary = PrimaryColor.Indigo;

		// Act
		SolidColorBrush result = primary.GetBrush();

		// Assert
		result
			.Should()
			.NotBeNull();

		result.Color
			.Should()
			.Be(SwatchHelper.Lookup[(MaterialColor)primary]);
	}

	/// <summary>
	/// Test of <see cref="MaterialDesignExtensions.GetBrush(SecondaryColor)" />.
	/// </summary>
	[Test]
	public void GetBrush_For_SecondaryColor_Returns_SolidColorBrush_With_Mapped_Color()
	{
		// Arrange
		const SecondaryColor secondary = SecondaryColor.Cyan;

		// Act
		SolidColorBrush result = secondary.GetBrush();

		// Assert
		result
			.Should()
			.NotBeNull();

		result.Color
			.Should()
			.Be(SwatchHelper.Lookup[(MaterialColor)secondary]);
	}
	#endregion
}
