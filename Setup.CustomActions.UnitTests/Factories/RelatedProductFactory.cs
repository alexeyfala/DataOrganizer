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
	/// Returns an installed product of <see cref="UpgradeCode" /> unless another upgrade code is given.
	/// </summary>
	public static RelatedProduct CreateProduct(
		string productCode,
		string version,
		string upgradeCode = UpgradeCode)
	{
		return new RelatedProduct
		{
			ProductCode = productCode,
			UpgradeCode = upgradeCode,
			Version = new Version(version)
		};
	}
	#endregion
}
