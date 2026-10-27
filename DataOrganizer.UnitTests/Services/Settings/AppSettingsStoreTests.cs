using AppInstance.Interfaces;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Settings;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services.Settings;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using Shared.Interfaces;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Settings;

[TestFixture(Description = $@"Tests of ""{nameof(AppSettingsStore)}"" type")]
internal class AppSettingsStoreTests
{
	#region Methods
	/// <summary>
	/// <see cref="IAppSettingsStore.CreateDefaultSettings" />: the default theme follows the system, with a teal primary
	/// color and an amber accent.
	/// </summary>
	[Test]
	public void CreateDefaultSettings_Follows_The_System_Theme_In_Teal_And_Amber()
	{
		// Act
		AppSettings result = IAppSettingsStore.CreateDefaultSettings();

		// Assert
		result.Theme
			.Should()
			.Be(ThemeMode.Inherit);

		result.PrimaryColor
			.Should()
			.Be(ThemePrimaryColor.Teal);

		result.SecondaryColor
			.Should()
			.Be(ThemeSecondaryColor.Amber);
	}

	/// <summary>
	/// <see cref="AppSettingsStore.Overwrite" />: replaces the current settings with the provided ones.
	/// </summary>
	[Test]
	public void Overwrite_Overwrites_Settings()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		using AutoMock mock = AutoMock.GetLoose();

		AppSettingsStore sut = mock.Create<AppSettingsStore>();

		// Act
		sut.Overwrite(settings);

		// Assert
		sut.Settings
			.Should()
			.BeEquivalentTo(settings);
	}

	/// <summary>
	/// <see cref="AppSettingsStore.Save" />: serializes the settings to a JSON file.
	/// </summary>
	[Test]
	public void Save_Serializes_Settings_To_File()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		AppSettingsStore sut = mock.Create<AppSettingsStore>(TypedParameter.From(fileSystem));

		// Act
		sut.Save();

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Any<AppSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="AppSettingsStore.Save" />: uses the settings file path obtained from the app environment.
	/// </summary>
	[Test]
	public void Save_Uses_Path_From_AppEnvironment()
	{
		// Arrange
		string expectedPath = TestPath.Combine("Settings", $"{nameof(AppSettings)}.json");

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(Arg.Any<string>())
				.Returns(expectedPath);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(appEnvironment);
		});

		AppSettingsStore sut = mock.Create<AppSettingsStore>();

		// Act
		sut.Save();

		// Assert
		fileSystem
			.Received(1)
			.SerializeToJsonFile(Arg.Any<AppSettings>(), expectedPath, false);
	}

	/// <summary>
	/// <see cref="AppSettingsStore" />: settings default to the created default settings when no file is present.
	/// </summary>
	[Test]
	public void Settings_Fall_Back_To_Defaults_Without_File()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		AppSettingsStore sut = mock.Create<AppSettingsStore>();

		// Assert
		sut.Settings
			.Should()
			.BeEquivalentTo(IAppSettingsStore.CreateDefaultSettings());
	}

	/// <summary>
	/// <see cref="AppSettingsStore" />: settings are loaded from the deserialized settings file.
	/// </summary>
	[Test]
	public void Settings_Obtained_From_File()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<AppSettings>(Arg.Any<string>())
				.Returns(settings);

			builder.RegisterInstance(serializer);
		});

		// Act
		AppSettingsStore sut = mock.Create<AppSettingsStore>();

		// Assert
		sut.Settings
			.Should()
			.BeEquivalentTo(settings);
	}
	#endregion
}
