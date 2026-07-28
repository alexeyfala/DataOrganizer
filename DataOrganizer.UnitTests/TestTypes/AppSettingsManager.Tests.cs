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
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(CreateStore(IAppSettingsManager.CreateDefaultSettings())));

		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Act
		Action act = sut.ApplyMaterialTheme;

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="AppSettingsManager.OverwriteSettings" />: overwrites the settings held by the store.
	/// </summary>
	[Test]
	public void OverwriteSettings_Overwrites_Settings_In_Store()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		IAppSettingsStore store = CreateStore(settings);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(store));

		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Act
		sut.OverwriteSettings(settings);

		// Assert
		store
			.Received()
			.Overwrite(settings);
	}

	/// <summary>
	/// <see cref="AppSettingsManager.SaveSettingsInFile" />: saves the settings through the store.
	/// </summary>
	[Test]
	public void SaveSettingsInFile_Saves_Settings_Through_Store()
	{
		// Arrange
		IAppSettingsStore store = CreateStore(TestUtils.CreateRandomSettings());

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(store));

		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Act
		sut.SaveSettingsInFile();

		// Assert
		store
			.Received()
			.Save();
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
	/// <see cref="AppSettingsManager.Settings" />: are the settings held by the store.
	/// </summary>
	[Test]
	public void Settings_Are_Obtained_From_Store()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(CreateStore(settings)));

		// Act
		AppSettingsManager sut = mock.Create<AppSettingsManager>();

		// Assert
		sut.Settings
			.Should()
			.BeSameAs(settings);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a store substitute holding the specified <paramref name="settings" />.
	/// </summary>
	private static IAppSettingsStore CreateStore(AppSettings settings)
	{
		IAppSettingsStore store = Substitute.For<IAppSettingsStore>();

		store
			.Settings
			.Returns(settings);

		return store;
	}
	#endregion
}
