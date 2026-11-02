using Setup.CustomActions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.CustomActions.Helpers;

/// <summary>
/// Matches installed products against the rows of the Upgrade table the way FindRelatedProducts does.
/// </summary>
public static class RelatedProductMatcher
{
	#region Data
	/// <summary>
	/// Separator of the product codes in the value of an action property.
	/// </summary>
	private const string ProductCodeSeparator = ";";
	#endregion

	#region Methods
	/// <summary>
	/// Returns the value of each action property: the codes of its matching products other than
	/// <paramref name="currentProductCode" />, joined by ';', or an empty string when none matches.
	/// </summary>
	public static IReadOnlyDictionary<string, string> Match(
		IReadOnlyCollection<UpgradeRule> rules,
		IReadOnlyCollection<RelatedProduct> products,
		string currentProductCode)
	{
		RelatedProduct[] others = [.. products
			.Where(x => !x.ProductCode.Equals(currentProductCode, StringComparison.OrdinalIgnoreCase))];

		Dictionary<string, string> properties = [];

		foreach (IGrouping<string, UpgradeRule> group in rules.GroupBy(static x => x.ActionProperty))
		{
			IEnumerable<string> productCodes = others
				.Where(x => group.Any(rule => IsMatch(rule, x)))
				.Select(static x => x.ProductCode);

			properties[group.Key] = string.Join(
				ProductCodeSeparator,
				productCodes);
		}

		return properties;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns whether a version is above the lower bound of a row, or equal to it when the bound includes itself.
	/// </summary>
	private static bool IsAboveMinimum(Version version, UpgradeRule rule)
	{
		if (rule.VersionMin is not { } minimum)
		{
			return true;
		}

		int order = version.CompareTo(ToComparedVersion(minimum));

		return order > 0 || (order == 0 && rule.Attributes.HasFlag(UpgradeAttributes.VersionMinInclusive));
	}

	/// <summary>
	/// Returns whether a version is below the upper bound of a row, or equal to it when the bound includes itself.
	/// </summary>
	private static bool IsBelowMaximum(Version version, UpgradeRule rule)
	{
		if (rule.VersionMax is not { } maximum)
		{
			return true;
		}

		int order = version.CompareTo(ToComparedVersion(maximum));

		return order < 0 || (order == 0 && rule.Attributes.HasFlag(UpgradeAttributes.VersionMaxInclusive));
	}

	/// <summary>
	/// Returns whether a product has the upgrade code of a row and a version within its bounds.
	/// </summary>
	private static bool IsMatch(UpgradeRule rule, RelatedProduct product)
	{
		if (!product.UpgradeCode.Equals(rule.UpgradeCode, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		Version version = ToComparedVersion(product.Version);

		return IsAboveMinimum(version, rule) && IsBelowMaximum(version, rule);
	}

	/// <summary>
	/// Returns a version the way Windows Installer compares it: the major, minor and build fields, without the fourth.
	/// </summary>
	private static Version ToComparedVersion(Version version)
	{
		return new Version(
			version.Major,
			version.Minor,
			Math.Max(version.Build, 0));
	}
	#endregion
}
