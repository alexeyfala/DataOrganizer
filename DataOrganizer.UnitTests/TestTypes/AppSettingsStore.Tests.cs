using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services.Settings;
using NSubstitute;
using Shared.Interfaces;
using System.IO;

namespace DataOrganizer.UnitTests.TestTypes;

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
		AppSettings settings = TestUtils.CreateRandomSettings();

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
		fileSystem.Received().SerializeToJsonFile(
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
		string expectedPath = Path.Combine(
			Path.GetTempPath(),
			"fake",
			$"{nameof(AppSettings)}.json");

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
			.Received()
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
		AppSettings settings = TestUtils.CreateRandomSettings();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.FromFile<AppSettings>(Arg.Any<string>())
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
