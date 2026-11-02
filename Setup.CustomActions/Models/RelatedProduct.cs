using Setup.CustomActions.Enums;
using System;

namespace Setup.CustomActions.Models;

/// <summary>
/// An installed product that shares an upgrade code with the package being installed.
/// </summary>
public sealed record RelatedProduct
{
	#region Properties
	/// <summary>
	/// Product code of the installed product.
	/// </summary>
	public required string ProductCode { get; init; }

	/// <summary>
	/// Installation context of the install.
	/// </summary>
	public required InstallScope Scope { get; init; }

	/// <summary>
	/// Upgrade code that relates the product to the package.
	/// </summary>
	public required string UpgradeCode { get; init; }

	/// <summary>
	/// Installed version of the product.
	/// </summary>
	public required Version Version { get; init; }
	#endregion
}
