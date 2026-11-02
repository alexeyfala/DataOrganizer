using AwesomeAssertions;
using Setup.CustomActions.Helpers;
using System.Collections.Generic;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.CustomActions.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(FeatureStateMigration)}"" type")]
internal class FeatureStateMigrationTests
{
	#region Data
	/// <summary>
	/// Name of a feature of the package.
	/// </summary>
	private const string FeatureName = "Autostart";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FeatureStateMigration.SelectStates" />: a feature the installed version has locally or has left out
	/// keeps that state.
	/// </summary>
	[TestCase(InstallState.Absent)]
	[TestCase(InstallState.Local)]
	public void SelectStates_Keeps_A_Local_Or_Absent_Feature(InstallState state)
	{
		// Arrange
		Dictionary<string, InstallState> installedStates = new()
		{
			[FeatureName] = state
		};

		// Act
		IReadOnlyDictionary<string, InstallState> result = FeatureStateMigration.SelectStates(installedStates);

		// Assert
		result
			.Should()
			.BeEquivalentTo(installedStates);
	}

	/// <summary>
	/// <see cref="FeatureStateMigration.SelectStates" />: a feature the installed version does not know, or holds in
	/// another state, keeps the package's own default.
	/// </summary>
	[TestCase(InstallState.Advertised)]
	[TestCase(InstallState.Source)]
	[TestCase(InstallState.Unknown)]
	public void SelectStates_Skips_A_Feature_Neither_Local_Nor_Absent(InstallState state)
	{
		// Arrange
		Dictionary<string, InstallState> installedStates = new()
		{
			[FeatureName] = state
		};

		// Act
		IReadOnlyDictionary<string, InstallState> result = FeatureStateMigration.SelectStates(installedStates);

		// Assert
		result
			.Should()
			.BeEmpty();
	}
	#endregion
}
