using AwesomeAssertions;
using Setup.CustomActions.Enums;
using Setup.CustomActions.Helpers;
using Setup.CustomActions.Models;
using Setup.CustomActions.UnitTests.Factories;
using System.Collections.Generic;

namespace Setup.CustomActions.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(OtherScopeInstallFinder)}"" type")]
internal class OtherScopeInstallFinderTests
{
	#region Data
	/// <summary>
	/// Product code of an installed product.
	/// </summary>
	private const string FirstProductCode = "{4736F185-B22D-4C6D-9B6E-362191BC9E74}";

	/// <summary>
	/// Product code of a product installed in the context of the install.
	/// </summary>
	private const string SameScopeProductCode = "{9014B628-2C41-4C09-9413-FA04AF306260}";

	/// <summary>
	/// Product code of a second installed product.
	/// </summary>
	private const string SecondProductCode = "{01F9DEEC-C704-42A3-9780-1E05CA83480B}";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="OtherScopeInstallFinder.Find" />: the installs of the other context, of any version, go to the
	/// property of that context, and an install of the same context is left out.
	/// </summary>
	[TestCase(InstallScope.PerMachine, InstallScope.PerUser, OtherScopeInstallFinder.InstalledForCurrentUserProperty)]
	[TestCase(InstallScope.PerUser, InstallScope.PerMachine, OtherScopeInstallFinder.InstalledForAllUsersProperty)]
	public void Find_Puts_The_Installs_Of_The_Other_Scope_Into_Its_Property(
		InstallScope scope,
		InstallScope otherScope,
		string expectedProperty)
	{
		// Arrange
		RelatedProduct[] products =
		[
			RelatedProductFactory.CreateProduct(FirstProductCode, "0.2.9", scope: otherScope),
			RelatedProductFactory.CreateProduct(SecondProductCode, "0.3.1", scope: otherScope),
			RelatedProductFactory.CreateProduct(SameScopeProductCode, "0.3.0", scope: scope)
		];

		Dictionary<string, string> expected = new()
		{
			[expectedProperty] = $"{FirstProductCode};{SecondProductCode}"
		};

		// Act
		IReadOnlyDictionary<string, string> result = OtherScopeInstallFinder.Find(scope, products);

		// Assert
		result
			.Should()
			.BeEquivalentTo(expected);
	}

	/// <summary>
	/// <see cref="OtherScopeInstallFinder.Find" />: without installs of the other context no property is set.
	/// </summary>
	[TestCase(InstallScope.PerMachine)]
	[TestCase(InstallScope.PerUser)]
	public void Find_Sets_No_Property_Without_Installs_Of_The_Other_Scope(InstallScope scope)
	{
		// Arrange
		RelatedProduct[] products = [RelatedProductFactory.CreateProduct(SameScopeProductCode, "0.2.9", scope: scope)];

		// Act
		IReadOnlyDictionary<string, string> result = OtherScopeInstallFinder.Find(scope, products);

		// Assert
		result
			.Should()
			.BeEmpty();
	}
	#endregion
}
