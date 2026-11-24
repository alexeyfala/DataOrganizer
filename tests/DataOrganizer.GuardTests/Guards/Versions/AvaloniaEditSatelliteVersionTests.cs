using AvaloniaEdit;
using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System;
using System.IO;
using System.Reflection;

namespace DataOrganizer.GuardTests.Guards.Versions;

[Guard]
[TestFixture(Description = "Guards that the Russian satellite of AvaloniaEdit has the version of AvaloniaEdit.dll")]
internal sealed class AvaloniaEditSatelliteVersionTests
{
	#region Methods
	/// <summary>
	/// The Russian satellite has the version of AvaloniaEdit.dll, without which a single-file build does not load it.
	/// </summary>
	[Test]
	public void RussianSatellite_Matches_AvaloniaEdit_Version()
	{
		// Arrange
		string satellitePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "ru", "AvaloniaEdit.resources.dll");

		// Act
		Version? satelliteVersion = AssemblyName
			.GetAssemblyName(satellitePath)
			.Version;

		// Assert
		satelliteVersion
			.Should()
			.Be(typeof(TextEditor).Assembly.GetName().Version);
	}
	#endregion
}
