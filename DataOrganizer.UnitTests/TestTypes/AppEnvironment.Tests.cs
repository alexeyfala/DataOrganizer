using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using NSubstitute;
using Shared.Common;
using System.IO;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(AppEnvironment)}"" type")]
internal class AppEnvironmentTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="AppEnvironment" /> constructor for the first running instance.
	/// </summary>
	[Test]
	public void Constructor_Builds_Paths_For_First_Instance()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IProcessUtils processUtils = Substitute.For<IProcessUtils>();

			processUtils
				.GetAppProcessesCount()
				.Returns(1);

			builder.RegisterInstance(processUtils);
		});

		// Act
		AppEnvironment sut = mock.Create<AppEnvironment>();

		// Assert
		string root = IAppEnvironment.GetAppDataDirectoryPath();

		sut.AppDataDirectoryPath
			.Should()
			.Be(Path.Combine(root, "Data"));

		sut.DatabaseDirectoryPath
			.Should()
			.Be(Path.Combine(root, "Data", "Database"));

		sut.SandboxDirectoryPath
			.Should()
			.Be(Path.Combine(root, "Data", "Sandbox"));
	}

	/// <summary>
	/// Test of <see cref="AppEnvironment" /> constructor for the second running instance.
	/// </summary>
	[Test]
	public void Constructor_Suffixes_Paths_With_Instance_Number_When_Multiple_Instances()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IProcessUtils processUtils = Substitute.For<IProcessUtils>();

			processUtils
				.GetAppProcessesCount()
				.Returns(3);

			builder.RegisterInstance(processUtils);
		});

		// Act
		AppEnvironment sut = mock.Create<AppEnvironment>();

		// Assert
		string root = IAppEnvironment.GetAppDataDirectoryPath();

		sut.AppDataDirectoryPath
			.Should()
			.Be(Path.Combine(root, "Data (3)"));

		sut.DatabaseDirectoryPath
			.Should()
			.Be(Path.Combine(root, "Data (3)", "Database"));
	}

	/// <summary>
	/// Test of <see cref="AppEnvironment.GetAppInstanceName" />.
	/// </summary>
	[Test]
	public void GetAppInstanceName_Returns_Plain_Name_For_Single_Instance()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IProcessUtils processUtils = Substitute.For<IProcessUtils>();

			processUtils
				.GetAppProcessesCount()
				.Returns(1);

			builder.RegisterInstance(processUtils);
		});

		AppEnvironment sut = mock.Create<AppEnvironment>();

		// Act
		string result = sut.GetAppInstanceName();

		// Assert
		result
			.Should()
			.Be(AppUtils.AppName);
	}

	/// <summary>
	/// Test of <see cref="AppEnvironment.GetAppInstanceName" />.
	/// </summary>
	[Test]
	public void GetAppInstanceName_Suffixes_Name_With_Instance_Number_When_Multiple_Instances()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IProcessUtils processUtils = Substitute.For<IProcessUtils>();

			processUtils
				.GetAppProcessesCount()
				.Returns(2);

			builder.RegisterInstance(processUtils);
		});

		AppEnvironment sut = mock.Create<AppEnvironment>();

		// Act
		string result = sut.GetAppInstanceName();

		// Assert
		result
			.Should()
			.Be($"{AppUtils.AppName} (2)");
	}

	/// <summary>
	/// Test of <see cref="AppEnvironment.GetSettingsFilePath" />.
	/// </summary>
	[Test]
	public void GetSettingsFilePath_Combines_Paths_Adds_Settings_Folder_And_Json_Extension()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IProcessUtils processUtils = Substitute.For<IProcessUtils>();

			processUtils
				.GetAppProcessesCount()
				.Returns(1);

			builder.RegisterInstance(processUtils);
		});

		AppEnvironment sut = mock.Create<AppEnvironment>();

		// Act
		string result = sut.GetSettingsFilePath("AppSettings");

		// Assert
		result
			.Should()
			.Be(Path.Combine(sut.AppDataDirectoryPath, "Settings", "AppSettings.json"));
	}
	#endregion
}
