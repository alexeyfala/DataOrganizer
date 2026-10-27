using AwesomeAssertions;
using DataOrganizer.Extensions.Settings;
using Settings.Enums;

namespace DataOrganizer.UnitTests.Extensions.Settings;

[TestFixture(Description = $@"Tests of ""{nameof(MaterialDesignExtensions)}"" type")]
internal class MaterialDesignExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="MaterialDesignExtensions.ToMaterial(ThemePrimaryColor)" />: each primary color gives the Material design
	/// color of its name.
	/// </summary>
	[Test]
	public void ToMaterial_Gives_The_Primary_Color_Of_The_Same_Name([Values] ThemePrimaryColor color)
	{
		// Act
		string result = color
			.ToMaterial()
			.ToString();

		// Assert
		result
			.Should()
			.Be(color.ToString());
	}

	/// <summary>
	/// <see cref="MaterialDesignExtensions.ToMaterial(ThemeSecondaryColor)" />: each accent color gives the Material design
	/// color of its name.
	/// </summary>
	[Test]
	public void ToMaterial_Gives_The_Secondary_Color_Of_The_Same_Name([Values] ThemeSecondaryColor color)
	{
		// Act
		string result = color
			.ToMaterial()
			.ToString();

		// Assert
		result
			.Should()
			.Be(color.ToString());
	}

	/// <summary>
	/// <see cref="MaterialDesignExtensions.ToMaterial(ThemeMode)" />: each theme mode gives the Material design mode of its
	/// name.
	/// </summary>
	[Test]
	public void ToMaterial_Gives_The_Theme_Mode_Of_The_Same_Name([Values] ThemeMode mode)
	{
		// Act
		string result = mode
			.ToMaterial()
			.ToString();

		// Assert
		result
			.Should()
			.Be(mode.ToString());
	}
	#endregion
}
