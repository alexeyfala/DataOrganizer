using System;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.CustomActions.Models;

/// <summary>
/// A row of the Upgrade table: the versions of one upgrade code and the property that collects the products found.
/// </summary>
public sealed record UpgradeRule
{
	#region Properties
	/// <summary>
	/// Property that receives the product codes of the matching products.
	/// </summary>
	public required string ActionProperty { get; init; }

	/// <summary>
	/// Bits that decide whether the bound versions themselves match.
	/// </summary>
	public required UpgradeAttributes Attributes { get; init; }

	/// <summary>
	/// Upgrade code of the products the row looks for.
	/// </summary>
	public required string UpgradeCode { get; init; }

	/// <summary>
	/// Upper bound of the matching versions, or <c>null</c> when there is none.
	/// </summary>
	public required Version? VersionMax { get; init; }

	/// <summary>
	/// Lower bound of the matching versions, or <c>null</c> when there is none.
	/// </summary>
	public required Version? VersionMin { get; init; }
	#endregion
}
