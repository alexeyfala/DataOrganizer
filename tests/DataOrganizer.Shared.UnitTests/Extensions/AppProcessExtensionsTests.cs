using AwesomeAssertions;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.TestSupport.Common;
using NSubstitute;

namespace DataOrganizer.Shared.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(AppProcessExtensions)}"" type")]
internal class AppProcessExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppProcessExtensions.GetEntryAssemblyPath" />: the entry assembly lies in the folder of the application.
	/// </summary>
	[Test]
	public void GetEntryAssemblyPath_Points_Into_The_App_Folder()
	{
		// Arrange
		IAppProcess appProcess = Substitute.For<IAppProcess>();

		appProcess
			.BaseDirectory
			.Returns(TestPath.Combine("Apps"));

		appProcess
			.EntryAssemblyName
			.Returns("DataOrganizer");

		// Act
		string? result = appProcess.GetEntryAssemblyPath();

		// Assert
		result
			.Should()
			.Be(TestPath.Combine("Apps", "DataOrganizer.dll"));
	}

	/// <summary>
	/// <see cref="AppProcessExtensions.GetEntryAssemblyPath" />: without the name of the entry assembly there is no path.
	/// </summary>
	[Test]
	public void GetEntryAssemblyPath_Without_A_Name_Gives_Nothing()
	{
		// Arrange
		IAppProcess appProcess = Substitute.For<IAppProcess>();

		appProcess
			.BaseDirectory
			.Returns(TestPath.Combine("Apps"));

		// The name is unknown.
		appProcess
			.EntryAssemblyName
			.Returns((string?)null);

		// Act
		string? result = appProcess.GetEntryAssemblyPath();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AppProcessExtensions.IsRunByDotnetHost" />: only the shared <c>dotnet</c> host counts, whatever its
	/// extension and case.
	/// </summary>
	[TestCase("dotnet", ExpectedResult = true)]
	[TestCase("dotnet.exe", ExpectedResult = true)]
	[TestCase("DotNet", ExpectedResult = true)]
	[TestCase("DataOrganizer", ExpectedResult = false)]
	[TestCase("DataOrganizer-0.3.0-linux-x64-portable", ExpectedResult = false)]
	public bool IsRunByDotnetHost_Tells_The_Host_By_Its_Name(string fileName)
	{
		// Arrange
		IAppProcess appProcess = Substitute.For<IAppProcess>();

		appProcess
			.ProcessPath
			.Returns(TestPath.Combine("bin", fileName));

		// Act
		return appProcess.IsRunByDotnetHost();
	}

	/// <summary>
	/// <see cref="AppProcessExtensions.IsRunByDotnetHost" />: an unknown path of the process is not the host.
	/// </summary>
	[Test]
	public void IsRunByDotnetHost_Without_A_Process_Path_Is_False()
	{
		// Arrange
		IAppProcess appProcess = Substitute.For<IAppProcess>();

		// The path is unknown.
		appProcess
			.ProcessPath
			.Returns((string?)null);

		// Act
		bool result = appProcess.IsRunByDotnetHost();

		// Assert
		result
			.Should()
			.BeFalse();
	}
	#endregion
}
