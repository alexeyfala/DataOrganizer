using AwesomeAssertions;
using Setup.CustomActions.Helpers;
using Setup.CustomActions.Models;
using Setup.CustomActions.UnitTests.Factories;
using System.Collections.Generic;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.CustomActions.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(RelatedProductMatcher)}"" type")]
internal class RelatedProductMatcherTests
{
	#region Data
	/// <summary>
	/// Product code of the package being installed.
	/// </summary>
	private const string CurrentProductCode = "{9014B628-2C41-4C09-9413-FA04AF306260}";

	/// <summary>
	/// Product code of an installed product.
	/// </summary>
	private const string FirstProductCode = "{4736F185-B22D-4C6D-9B6E-362191BC9E74}";

	/// <summary>
	/// Version of the package being installed.
	/// </summary>
	private const string PackageVersion = "0.3.0";

	/// <summary>
	/// Product code of a second installed product.
	/// </summary>
	private const string SecondProductCode = "{01F9DEEC-C704-42A3-9780-1E05CA83480B}";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="RelatedProductMatcher.Match" />: the codes of several matching products are joined in their order.
	/// </summary>
	[Test]
	public void Match_Joins_The_Codes_Of_Several_Products()
	{
		// Arrange
		RelatedProduct[] products =
		[
			RelatedProductFactory.CreateProduct(FirstProductCode, "0.2.0"),
			RelatedProductFactory.CreateProduct(SecondProductCode, "0.2.9")
		];

		// Act
		IReadOnlyDictionary<string, string> result = RelatedProductMatcher.Match(
			UpgradeRuleFactory.CreateMajorUpgradeRules(PackageVersion),
			products,
			CurrentProductCode);

		// Assert
		result[UpgradeRuleFactory.UpgradeProperty]
			.Should()
			.Be($"{FirstProductCode};{SecondProductCode}");
	}

	/// <summary>
	/// <see cref="RelatedProductMatcher.Match" />: an installed version goes to the property of its range — older and
	/// equal ones to the upgrade, newer ones to the downgrade — and the fourth field counts for nothing.
	/// </summary>
	[TestCase("0.2.9", UpgradeRuleFactory.UpgradeProperty)]
	[TestCase("0.3.0", UpgradeRuleFactory.UpgradeProperty)]
	[TestCase("0.3.0.7", UpgradeRuleFactory.UpgradeProperty)]
	[TestCase("0.3.1", UpgradeRuleFactory.DowngradeProperty)]
	public void Match_Puts_A_Version_Into_The_Property_Of_Its_Range(string installedVersion, string expectedProperty)
	{
		// Arrange
		RelatedProduct[] products = [RelatedProductFactory.CreateProduct(FirstProductCode, installedVersion)];

		Dictionary<string, string> expected = new()
		{
			[UpgradeRuleFactory.DowngradeProperty] = string.Empty,
			[UpgradeRuleFactory.UpgradeProperty] = string.Empty,
			[expectedProperty] = FirstProductCode
		};

		// Act
		IReadOnlyDictionary<string, string> result = RelatedProductMatcher.Match(
			UpgradeRuleFactory.CreateMajorUpgradeRules(PackageVersion),
			products,
			CurrentProductCode);

		// Assert
		result
			.Should()
			.BeEquivalentTo(expected);
	}

	/// <summary>
	/// <see cref="RelatedProductMatcher.Match" />: a product of another upgrade code matches no row, and every property
	/// is cleared.
	/// </summary>
	[Test]
	public void Match_Skips_A_Product_Of_Another_Upgrade_Code()
	{
		// Arrange
		RelatedProduct[] products =
		[
			RelatedProductFactory.CreateProduct(
				FirstProductCode,
				PackageVersion,
				upgradeCode: "{A6E8AC21-092B-4288-801A-CEA4130D7537}")
		];

		Dictionary<string, string> expected = new()
		{
			[UpgradeRuleFactory.DowngradeProperty] = string.Empty,
			[UpgradeRuleFactory.UpgradeProperty] = string.Empty
		};

		// Act
		IReadOnlyDictionary<string, string> result = RelatedProductMatcher.Match(
			UpgradeRuleFactory.CreateMajorUpgradeRules(PackageVersion),
			products,
			CurrentProductCode);

		// Assert
		result
			.Should()
			.BeEquivalentTo(expected);
	}

	/// <summary>
	/// <see cref="RelatedProductMatcher.Match" />: the product being installed is not an upgrade of itself, whatever
	/// the case of its code.
	/// </summary>
	[Test]
	public void Match_Skips_The_Product_Being_Installed()
	{
		// Arrange
		RelatedProduct[] products =
		[
			RelatedProductFactory.CreateProduct(
				CurrentProductCode.ToLowerInvariant(),
				PackageVersion)
		];

		// Act
		IReadOnlyDictionary<string, string> result = RelatedProductMatcher.Match(
			UpgradeRuleFactory.CreateMajorUpgradeRules(PackageVersion),
			products,
			CurrentProductCode);

		// Assert
		result[UpgradeRuleFactory.UpgradeProperty]
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="RelatedProductMatcher.Match" />: a version at both bounds of a row matches only when both bounds
	/// include themselves.
	/// </summary>
	[TestCase(UpgradeAttributes.OnlyDetect, "")]
	[TestCase(UpgradeAttributes.VersionMinInclusive, "")]
	[TestCase(UpgradeAttributes.VersionMaxInclusive, "")]
	[TestCase(UpgradeAttributes.VersionMinInclusive | UpgradeAttributes.VersionMaxInclusive, FirstProductCode)]
	public void Match_Takes_A_Bound_Version_Only_Through_Inclusive_Bounds(UpgradeAttributes attributes, string expectedCodes)
	{
		// Arrange
		RelatedProduct[] products = [RelatedProductFactory.CreateProduct(FirstProductCode, PackageVersion)];

		UpgradeRule[] rules = [UpgradeRuleFactory.CreateRule(attributes, PackageVersion)];

		// Act
		IReadOnlyDictionary<string, string> result = RelatedProductMatcher.Match(
			rules,
			products,
			CurrentProductCode);

		// Assert
		result[UpgradeRuleFactory.CustomProperty]
			.Should()
			.Be(expectedCodes);
	}
	#endregion
}
