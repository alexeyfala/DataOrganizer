using Setup.CustomActions.Enums;
using Setup.CustomActions.Models;
using System;

namespace Setup.CustomActions.UnitTests.Factories;

/// <summary>
/// Builds installed products for the tests of the upgrade search.
/// </summary>
internal static class RelatedProductFactory
{
	#region Data
	/// <summary>
	/// Upgrade code of the product of the tests.
	/// </summary>
	public const string UpgradeCode = "{C3A26395-83F5-47D2-A5EB-FBEA125E67E6}";
	#endregion

	#region Methods
	/// <summary>
	/// Returns a product installed for one user with <see cref="UpgradeCode" />, unless another context or upgrade code
	/// is given.
	/// </summary>
	public static RelatedProduct CreateProduct(
		string productCode,
		string version,
		string upgradeCode = UpgradeCode,
		InstallScope scope = InstallScope.PerUser)
	{
		return new RelatedProduct
		{
			ProductCode = productCode,
			Scope = scope,
			UpgradeCode = upgradeCode,
			Version = new Version(version)
		};
	}
	#endregion
}
