using Setup.CustomActions.Models;
using System;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.CustomActions.UnitTests.Factories;

/// <summary>
/// Builds rows of the Upgrade table for the tests of the upgrade search.
/// </summary>
internal static class UpgradeRuleFactory
{
	#region Data
	/// <summary>
	/// Property of a row built by <see cref="CreateRule" />.
	/// </summary>
	public const string CustomProperty = "CUSTOM_DETECTED";

	/// <summary>
	/// Property of the newer installed versions, which block the install.
	/// </summary>
	public const string DowngradeProperty = "WIX_DOWNGRADE_DETECTED";

	/// <summary>
	/// Property of the older and equal installed versions, which the install replaces.
	/// </summary>
	public const string UpgradeProperty = "WIX_UPGRADE_DETECTED";
	#endregion

	#region Methods
	/// <summary>
	/// Returns the two rows that MajorUpgrade with same-version upgrades writes for a package of <paramref name="version" />.
	/// </summary>
	public static UpgradeRule[] CreateMajorUpgradeRules(string version)
	{
		return
		[
			new UpgradeRule
			{
				ActionProperty = UpgradeProperty,
				Attributes = UpgradeAttributes.MigrateFeatures | UpgradeAttributes.VersionMaxInclusive,
				UpgradeCode = RelatedProductFactory.UpgradeCode,
				VersionMax = new Version(version),
				VersionMin = null
			},
			new UpgradeRule
			{
				ActionProperty = DowngradeProperty,
				Attributes = UpgradeAttributes.OnlyDetect,
				UpgradeCode = RelatedProductFactory.UpgradeCode,
				VersionMax = null,
				VersionMin = new Version(version)
			}
		];
	}

	/// <summary>
	/// Returns a row of <see cref="CustomProperty" /> with both bounds at <paramref name="version" />.
	/// </summary>
	public static UpgradeRule CreateRule(UpgradeAttributes attributes, string version)
	{
		return new UpgradeRule
		{
			ActionProperty = CustomProperty,
			Attributes = attributes,
			UpgradeCode = RelatedProductFactory.UpgradeCode,
			VersionMax = new Version(version),
			VersionMin = new Version(version)
		};
	}
	#endregion
}
