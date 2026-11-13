using Setup.CustomActions.Enums;
using Setup.CustomActions.Models;
using System.Collections.Generic;
using System.Linq;

namespace Setup.CustomActions.Helpers;

/// <summary>
/// Finds the installs of the product in the other installation context, which a major upgrade cannot replace.
/// </summary>
public static class OtherScopeInstallFinder
{
	#region Data
	/// <summary>
	/// Property of the installs for all users, found while the product is installed for one user.
	/// </summary>
	public const string InstalledForAllUsersProperty = "InstalledForAllUsers";

	/// <summary>
	/// Property of the installs for the current user, found while the product is installed for all users.
	/// </summary>
	public const string InstalledForCurrentUserProperty = "InstalledForCurrentUser";
	#endregion

	#region Methods
	/// <summary>
	/// Returns the property of the other context with the codes of its installs joined by ';', or no property when
	/// the product is installed in no other context, whatever its version.
	/// </summary>
	public static IReadOnlyDictionary<string, string> Find(InstallScope scope, IReadOnlyCollection<RelatedProduct> products)
	{
		RelatedProduct[] others = [.. products.Where(x => x.Scope != scope)];

		Dictionary<string, string> properties = [];

		if (others.Length == 0)
		{
			return properties;
		}

		string property = scope == InstallScope.PerUser
			? InstalledForAllUsersProperty
			: InstalledForCurrentUserProperty;

		properties[property] = ProductCodeList.Join(others);

		return properties;
	}
	#endregion
}
