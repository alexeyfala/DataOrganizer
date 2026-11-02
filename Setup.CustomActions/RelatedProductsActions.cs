using Setup.CustomActions.Enums;
using Setup.CustomActions.Helpers;
using Setup.CustomActions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.CustomActions;

/// <summary>
/// Custom actions that find the installed versions of the product and carry their feature states over.
/// </summary>
public static class RelatedProductsActions
{
	#region Data
	/// <summary>
	/// Query of the rows of the Upgrade table, in the column order the reader expects.
	/// </summary>
	private const string UpgradeQuery = "SELECT `UpgradeCode`, `VersionMin`, `VersionMax`, `Attributes`, `ActionProperty` FROM `Upgrade`";
	#endregion

	#region Methods
	/// <summary>
	/// Finds the installs of the product in the other installation context, which a major upgrade cannot replace.
	/// </summary>
	[CustomAction]
	public static ActionResult FindOtherScopeInstalls(Session session)
	{
		return Run(
			session,
			nameof(FindOtherScopeInstalls),
			() => SetProperties(session, nameof(FindOtherScopeInstalls), FindOtherScopeProperties(session)));
	}

	/// <summary>
	/// Repeats FindRelatedProducts for the installs of the current user. The search of the dialogs runs before the scope
	/// is chosen, so it sees only installs for all users, and Windows Installer never repeats it.
	/// </summary>
	[CustomAction]
	public static ActionResult FindPerUserRelatedProducts(Session session)
	{
		return Run(
			session,
			nameof(FindPerUserRelatedProducts),
			() => SetProperties(session, nameof(FindPerUserRelatedProducts), FindPerUserProperties(session)));
	}

	/// <summary>
	/// Repeats MigrateFeatureStates for a per-user choice: the dialogs carry the feature states over before the scope
	/// is chosen, so only from installs for all users.
	/// </summary>
	[CustomAction]
	public static ActionResult MigratePerUserFeatureStates(Session session)
	{
		return Run(
			session,
			nameof(MigratePerUserFeatureStates),
			() => MigratePerUserFeatures(session));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the property that names the installs of the other context, if there are any.
	/// </summary>
	private static IReadOnlyDictionary<string, string> FindOtherScopeProperties(Session session)
	{
		RelatedProduct[] products = [.. GetInstalledProducts(session, [session["UpgradeCode"]])];

		return OtherScopeInstallFinder.Find(
			GetScope(session),
			products);
	}

	/// <summary>
	/// Returns the action properties of the Upgrade table for the installs of the current user.
	/// </summary>
	private static IReadOnlyDictionary<string, string> FindPerUserProperties(Session session)
	{
		UpgradeRule[] rules = ReadUpgradeRules(session.Database);

		return RelatedProductMatcher.Match(
			rules,
			GetPerUserProducts(session, rules),
			session["ProductCode"]);
	}

	/// <summary>
	/// Returns the installs of the products of the upgrade codes, for all users and for the current user.
	/// </summary>
	private static IEnumerable<RelatedProduct> GetInstalledProducts(Session session, IEnumerable<string> upgradeCodes)
	{
		// Without the SID of the user Windows Installer reads no properties of a per-user install
		string userSid = session["UserSID"];

		foreach (string upgradeCode in upgradeCodes.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			IEnumerable<string> productCodes = ProductInstallation
				.GetRelatedProducts(upgradeCode)
				.Select(static x => x.ProductCode)
				.Distinct(StringComparer.OrdinalIgnoreCase);

			foreach (string productCode in productCodes)
			{
				// The search names a product but not its context, so each context is read
				foreach (InstallScope scope in new[] { InstallScope.PerMachine, InstallScope.PerUser })
				{
					if (ReadInstall(productCode, upgradeCode, scope, userSid) is { } product)
					{
						yield return product;
					}
				}
			}
		}
	}

	/// <summary>
	/// Returns the installs of the current user of the products of the Upgrade table.
	/// </summary>
	private static RelatedProduct[] GetPerUserProducts(Session session, IReadOnlyCollection<UpgradeRule> rules)
	{
		return [.. GetInstalledProducts(session, rules.Select(static x => x.UpgradeCode))
			.Where(static x => x.Scope == InstallScope.PerUser)];
	}

	/// <summary>
	/// Returns the context of the running install. Windows Installer clears ALLUSERS once it installs for one user.
	/// </summary>
	private static InstallScope GetScope(Session session)
	{
		return string.IsNullOrEmpty(session["ALLUSERS"])
			? InstallScope.PerUser
			: InstallScope.PerMachine;
	}

	/// <summary>
	/// Requests the feature states of the installed version of the current user that the package upgrades, if any.
	/// </summary>
	private static void MigratePerUserFeatures(Session session)
	{
		UpgradeRule[] rules = ReadUpgradeRules(session.Database);

		RelatedProduct? source = RelatedProductMatcher.FindFeatureSource(
			rules,
			GetPerUserProducts(session, rules),
			session["ProductCode"]);

		if (source is null)
		{
			session.Log($"{nameof(MigratePerUserFeatureStates)}: no installed version to take the features from");

			return;
		}

		Dictionary<string, InstallState> installedStates = session.Features.ToDictionary(
			static x => x.Name,
			x => new FeatureInstallation(x.Name, source.ProductCode).State);

		foreach (KeyValuePair<string, InstallState> state in FeatureStateMigration.SelectStates(installedStates))
		{
			session.Features[state.Key].RequestState = state.Value;

			session.Log($"{nameof(MigratePerUserFeatureStates)}: {state.Key} = {state.Value} from {source.ProductCode}");
		}
	}

	/// <summary>
	/// Returns a version stored in the Upgrade table, or <c>null</c> for an empty cell.
	/// </summary>
	private static Version? ParseVersion(string text) => string.IsNullOrEmpty(text) ? null : new Version(text);

	/// <summary>
	/// Returns the install of a product in one context, or <c>null</c> when it is not installed there.
	/// </summary>
	private static RelatedProduct? ReadInstall(
		string productCode,
		string upgradeCode,
		InstallScope scope,
		string userSid)
	{
		ProductInstallation installation = scope == InstallScope.PerMachine
			? new ProductInstallation(productCode, null, UserContexts.Machine)
			: new ProductInstallation(productCode, userSid, UserContexts.UserUnmanaged);

		// IsInstalled holds in any context, but only an install has a version in its own context
		if (installation.ProductVersion is not { } version)
		{
			return null;
		}

		return new RelatedProduct
		{
			ProductCode = productCode,
			Scope = scope,
			UpgradeCode = upgradeCode,
			Version = version
		};
	}

	/// <summary>
	/// Returns the rows of the Upgrade table of the running package.
	/// </summary>
	private static UpgradeRule[] ReadUpgradeRules(Database database)
	{
		using View view = database.OpenView(UpgradeQuery);

		view.Execute();

		List<UpgradeRule> rules = [];

		foreach (Record record in view)
		{
			using (record)
			{
				rules.Add(new UpgradeRule
				{
					ActionProperty = record.GetString(5),
					Attributes = (UpgradeAttributes)record.GetInteger(4),
					UpgradeCode = record.GetString(1),
					VersionMax = ParseVersion(record.GetString(3)),
					VersionMin = ParseVersion(record.GetString(2))
				});
			}
		}

		return [.. rules];
	}

	/// <summary>
	/// Runs the work of an action. A failure is logged, and the install goes on without its result.
	/// </summary>
	private static ActionResult Run(
		Session session,
		string actionName,
		Action work)
	{
		try
		{
			work();
		}
		catch (Exception e)
		{
			// Without the result the install still goes on, as it would without the action
			session.Log($"{actionName} failed: {e}");
		}

		return ActionResult.Success;
	}

	/// <summary>
	/// Sets the properties an action has found and logs each.
	/// </summary>
	private static void SetProperties(
		Session session,
		string actionName,
		IReadOnlyDictionary<string, string> properties)
	{
		if (properties.Count == 0)
		{
			session.Log($"{actionName}: nothing found");
		}

		foreach (KeyValuePair<string, string> property in properties)
		{
			session[property.Key] = property.Value;

			session.Log($"{actionName}: {property.Key} = '{property.Value}'");
		}
	}
	#endregion
}
