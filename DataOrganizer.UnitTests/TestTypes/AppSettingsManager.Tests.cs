using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using NSubstitute;
using Shared.Interfaces;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(AppSettingsManager)}"" type")]
internal class AppSettingsManagerTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="AppSettingsManager" />.
	/// </summary>
	[Test]
	public void Obtained_Default_Settings()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.FromFile<AppSettings>(Arg.Any<string>())
				.Returns(default(AppSettings));

			builder.RegisterInstance(serializer);
		});

		// Act
		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Assert
		sut.Settings
			.Should()
			.BeEquivalentTo(IAppSettingsManager.CreateDefaultSettings());
	}

	/// <summary>
	/// Test of <see cref="AppSettingsManager.OverwriteSettings" />.
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
	/// Test of <see cref="AppSettingsManager.SaveSettingsInFile" />.
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
	/// Test of <see cref="AppSettingsManager" />.
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
