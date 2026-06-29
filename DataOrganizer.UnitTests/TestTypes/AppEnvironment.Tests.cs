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
	#region Data
	/// <summary>
	/// Default directory name.
	/// </summary>
	private const string DefaultDirectoryName = "Instance";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="AppEnvironment" /> constructor when first running instance.
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
			.Be(Path.Combine(root, DefaultDirectoryName));

		sut.DatabaseDirectoryPath
			.Should()
			.Be(Path.Combine(root, DefaultDirectoryName, "Database"));

		sut.SandboxDirectoryPath
			.Should()
			.Be(Path.Combine(root, DefaultDirectoryName, "Sandbox"));
	}

	/// <summary>
	/// <see cref="AppEnvironment" /> constructor when second running instance.
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
				.Returns(2);

			builder.RegisterInstance(processUtils);
		});

		// Act
		AppEnvironment sut = mock.Create<AppEnvironment>();

		// Assert
		string root = IAppEnvironment.GetAppDataDirectoryPath();

		sut.AppDataDirectoryPath
			.Should()
			.Be(Path.Combine(root, $"{DefaultDirectoryName} (2)"));

		sut.DatabaseDirectoryPath
			.Should()
			.Be(Path.Combine(root, $"{DefaultDirectoryName} (2)", "Database"));
	}

	/// <summary>
	/// <see cref="AppEnvironment.GetAppInstanceName" />: returns the plain application name for a single instance.
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
			.Be(AppUtils.AppNameParted);
	}

	/// <summary>
	/// <see cref="AppEnvironment.GetAppInstanceName" />: suffixes the name with the instance number when multiple instances run.
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
			.Be($"{AppUtils.AppNameParted} (2)");
	}

	/// <summary>
	/// <see cref="AppEnvironment.GetSettingsFilePath" />: combines the app data path with the Settings folder and a .json extension.
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
