using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using DataOrganizer.Infrastructure.Settings.Dto;
using DataOrganizer.Infrastructure.Settings.Helpers;
using DataOrganizer.Infrastructure.Settings.Interfaces;
using DataOrganizer.Infrastructure.Settings.Services;
using DataOrganizer.Infrastructure.UnitTests.Settings.TestHelpers.Factories;
using DataOrganizer.TestSupport.Common;
using NSubstitute;

namespace DataOrganizer.Infrastructure.UnitTests.Settings.Services;

[TestFixture(Description = $@"Tests of ""{nameof(AppSettingsStore)}"" type")]
internal class AppSettingsStoreTests
{
	#region Methods
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
		IJsonFileStore jsonFileStore = Substitute.For<IJsonFileStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(jsonFileStore));

		AppSettingsStore sut = mock.Create<AppSettingsStore>();

		// Act
		sut.Save();

		// Assert
		jsonFileStore
			.Received(1)
			.Write(sut.Settings, Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="AppSettingsStore.Save" />: uses the settings file path obtained from the app environment.
	/// </summary>
	[Test]
	public void Save_Uses_Path_From_AppEnvironment()
	{
		// Arrange
		string expectedPath = TestPath.Combine("Settings", $"{nameof(AppSettings)}.json");

		IJsonFileStore jsonFileStore = Substitute.For<IJsonFileStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(Arg.Any<string>())
				.Returns(expectedPath);

			builder.RegisterInstance(jsonFileStore);

			builder.RegisterInstance(appEnvironment);
		});

		AppSettingsStore sut = mock.Create<AppSettingsStore>();

		// Act
		sut.Save();

		// Assert
		jsonFileStore
			.Received(1)
			.Write(Arg.Any<AppSettings>(), expectedPath);
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
			.BeEquivalentTo(AppSettingsDefaults.Create());
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
			IJsonFileStore jsonFileStore = Substitute.For<IJsonFileStore>();

			jsonFileStore
				.Read<AppSettings>(Arg.Any<string>())
				.Returns(settings);

			builder.RegisterInstance(jsonFileStore);
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
