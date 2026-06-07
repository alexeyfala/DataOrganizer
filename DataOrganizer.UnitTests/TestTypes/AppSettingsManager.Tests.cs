using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using Material.Colors;
using Material.Styles.Themes.Base;
using NSubstitute;
using Shared.Interfaces;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(AppSettingsManager)}"" type")]
internal class AppSettingsManagerTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppSettingsManager.ApplyMaterialTheme" />: does not throw when running under NUnit.
	/// </summary>
	[Test]
	public void ApplyMaterialTheme_Does_Not_Throw_When_Running_Under_NUnit()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Act
		Action act = sut.ApplyMaterialTheme;

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="AppSettingsManager" />: settings default to the created default settings when no file is present.
	/// </summary>
	[Test]
	public void Obtained_Default_Settings()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Assert
		sut.Settings
			.Should()
			.BeEquivalentTo(IAppSettingsManager.CreateDefaultSettings());
	}

	/// <summary>
	/// <see cref="AppSettingsManager.OverwriteSettings" />: replaces the current settings with the provided ones.
	/// </summary>
	[Test]
	public void OverwriteSettings_Overwrites_Settings()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		using AutoMock mock = AutoMock.GetLoose();

		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Act
		sut.OverwriteSettings(settings);

		// Assert
		sut.Settings
			.Should()
			.BeEquivalentTo(settings);
	}

	/// <summary>
	/// <see cref="AppSettingsManager.SaveSettingsInFile" />: serializes the settings to a JSON file.
	/// </summary>
	[Test]
	public void SaveSettingsInFile_Saves_Settings_In_File()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		AppSettingsManager sut = mock.Create<AppSettingsManager>(TypedParameter.From(fileSystem));

		// Act
		sut.SaveSettingsInFile();

		// Assert
		fileSystem.Received().SerializeToJsonFile(
			Arg.Any<AppSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="AppSettingsManager.SaveSettingsInFile" />: uses the settings file path obtained from the app environment.
	/// </summary>
	[Test]
	public void SaveSettingsInFile_Uses_Path_From_AppEnvironment()
	{
		// Arrange
		const string expectedPath = @"C:\fake\AppSettings.json";

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

		appEnvironment
			.GetSettingsFilePath(Arg.Any<string>())
			.Returns(expectedPath);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(appEnvironment);
		});

		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Act
		sut.SaveSettingsInFile();

		// Assert
		fileSystem
			.Received()
			.SerializeToJsonFile(Arg.Any<AppSettings>(), expectedPath, false);
	}

	/// <summary>
	/// <see cref="AppSettingsManager.SetAppMaterialTheme" />: is a no-op and does not throw when running under NUnit.
	/// </summary>
	[Test]
	public void SetAppMaterialTheme_Is_NoOp_When_Running_Under_NUnit()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Act
		Action act = () => sut.SetAppMaterialTheme(
			BaseThemeMode.Dark,
			PrimaryColor.Indigo,
			SecondaryColor.Cyan);

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="AppSettingsManager" />: settings are loaded from the deserialized settings file.
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
		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Assert
		sut.Settings
			.Should()
			.BeEquivalentTo(settings);
	}
	#endregion
}
