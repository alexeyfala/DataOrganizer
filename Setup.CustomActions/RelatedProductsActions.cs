using Setup.CustomActions.Helpers;
using Setup.CustomActions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.CustomActions;

/// <summary>
/// Custom actions that find the installed versions of the product.
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
	/// Repeats FindRelatedProducts for the installs of the current user. The search of the dialogs runs before the scope
	/// is chosen, so it sees only installs for all users, and Windows Installer never repeats it.
	/// </summary>
	[CustomAction]
	public static ActionResult FindPerUserRelatedProducts(Session session)
	{
		try
		{
			UpgradeRule[] rules = ReadUpgradeRules(session.Database);

			RelatedProduct[] products = [.. rules
				.Select(static x => x.UpgradeCode)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.SelectMany(GetPerUserProducts)];

			IReadOnlyDictionary<string, string> properties = RelatedProductMatcher.Match(
				rules,
				products,
				session["ProductCode"]);

			foreach (KeyValuePair<string, string> property in properties)
			{
				session[property.Key] = property.Value;

				session.Log($"{nameof(FindPerUserRelatedProducts)}: {property.Key} = '{property.Value}'");
			}
		}
		catch (Exception e)
		{
			// Without the search the install still goes on, only beside an installed version
			session.Log($"{nameof(FindPerUserRelatedProducts)} failed: {e}");
		}

		return ActionResult.Success;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the products of an upgrade code that the current user has installed for themselves.
	/// </summary>
	private static IEnumerable<RelatedProduct> GetPerUserProducts(string upgradeCode)
	{
		// Without the SID of the user Windows Installer reads no properties of a per-user install
		string? userSid = WindowsIdentity
			.GetCurrent()
			.User?
			.Value;

		foreach (ProductInstallation related in ProductInstallation.GetRelatedProducts(upgradeCode))
		{
			ProductInstallation installation = new(
				related.ProductCode,
				userSid,
				UserContexts.UserUnmanaged);

			// IsInstalled holds in any context, but only a per-user install has a version in this one
			if (installation.ProductVersion is not { } version)
			{
				continue;
			}

			yield return new RelatedProduct
			{
				ProductCode = installation.ProductCode,
				UpgradeCode = upgradeCode,
				Version = version
			};
		}
	}

	/// <summary>
	/// Returns a version stored in the Upgrade table, or <c>null</c> for an empty cell.
	/// </summary>
	private static Version? ParseVersion(string text) => string.IsNullOrEmpty(text) ? null : new Version(text);

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
	#endregion
}
