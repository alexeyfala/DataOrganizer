using AwesomeAssertions;
using DataOrganizer.Infrastructure.AppInstance.Helpers;
using DataOrganizer.Shared.Common;
using System;
using System.IO;

namespace DataOrganizer.Infrastructure.UnitTests.AppInstance.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(AppDataDirectory)}"" type")]
internal sealed class AppDataDirectoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppDataDirectory.GetPath" />: the data lie in the local application data of the user, and a Debug build
	/// keeps its own apart from those of the installed app.
	/// </summary>
	[Test]
	public void GetPath_Keeps_The_Data_Of_A_Debug_Build_Apart()
	{
		// Act
		string path = AppDataDirectory.GetPath();

		// Assert
		Path.GetDirectoryName(path)
			.Should()
			.Be(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

		Path.GetFileName(path)
			.Should()
			.Be(AppInfo.IsDebug ? "DataOrganizer_Debug" : "DataOrganizer");
	}
	#endregion
}
