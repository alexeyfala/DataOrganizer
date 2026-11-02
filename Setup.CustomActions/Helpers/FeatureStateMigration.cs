using System.Collections.Generic;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.CustomActions.Helpers;

/// <summary>
/// Carries the feature states of an installed version over to the package being installed.
/// </summary>
public static class FeatureStateMigration
{
	#region Methods
	/// <summary>
	/// Returns the states to request: the features installed locally or left out, as the installed version has them.
	/// Features it does not know are left to the package's own defaults.
	/// </summary>
	public static IReadOnlyDictionary<string, InstallState> SelectStates(IReadOnlyDictionary<string, InstallState> installedStates)
	{
		return installedStates
			.Where(static x => x.Value is InstallState.Local or InstallState.Absent)
			.ToDictionary(static x => x.Key, static x => x.Value);
	}
	#endregion
}
