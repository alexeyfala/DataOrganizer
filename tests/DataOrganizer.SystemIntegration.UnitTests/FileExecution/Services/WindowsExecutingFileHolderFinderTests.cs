using AwesomeAssertions;
using DataOrganizer.SystemIntegration.FileExecution.Services;
using System.Runtime.Versioning;

namespace DataOrganizer.SystemIntegration.UnitTests.FileExecution.Services;

[SupportedOSPlatform("windows")]
[TestFixture(Description = $@"Tests of ""{nameof(WindowsExecutingFileHolderFinder)}"" type")]
internal class WindowsExecutingFileHolderFinderTests
{
	#region Methods
	/// <summary>
	/// <see cref="WindowsExecutingFileHolderFinder.IsHolderToKill" />: this application is never killed, whatever kind
	/// the Restart Manager gives it.
	/// </summary>
	[Test]
	public void IsHolderToKill_Leaves_This_Application_Alone()
	{
		// Act
		bool result = WindowsExecutingFileHolderFinder.IsHolderToKill(1, 100, 100);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="WindowsExecutingFileHolderFinder.IsHolderToKill" />: programs of every kind are killed, but services,
	/// Explorer and critical processes are left alone.
	/// </summary>
	[TestCase(0, ExpectedResult = true)] // RmUnknownApp
	[TestCase(1, ExpectedResult = true)] // RmMainWindow
	[TestCase(2, ExpectedResult = true)] // RmOtherWindow
	[TestCase(3, ExpectedResult = false)] // RmService
	[TestCase(4, ExpectedResult = false)] // RmExplorer
	[TestCase(5, ExpectedResult = true)] // RmConsole
	[TestCase(1000, ExpectedResult = false)] // RmCritical
	public bool IsHolderToKill_Leaves_Services_Explorer_And_Critical_Processes_Alone(int applicationType)
	{
		return WindowsExecutingFileHolderFinder.IsHolderToKill(applicationType, 100, 200);
	}
	#endregion
}
