using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using Material.Colors;
using Material.Styles.Themes.Base;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsViewModel)}"" type")]
internal class SettingsViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="SettingsViewModel.Language" />.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_Language()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		using AutoMock mock = AutoMock.GetLoose();

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.Language = new(settings.Language);

		// Assert
		sut.CurrentSettings.Language
			.Should()
			.Be(settings.Language);
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.PrimaryColor" />.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_PrimaryColor()
	{
		// Arrange
		const PrimaryColor primaryColor = PrimaryColor.Red;

		IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

		using AutoMock mock = AutoMock.GetLoose();

		SettingsViewModel sut = mock.Create<SettingsViewModel>(TypedParameter.From(settingsManager));

		// Act
		sut.PrimaryColor = primaryColor;

		// Assert
		sut.PrimaryColor
			.Should()
			.Be(primaryColor);

		settingsManager.Received().SetAppMaterialTheme(
			Arg.Any<BaseThemeMode>(),
			Arg.Any<PrimaryColor>(),
			Arg.Any<SecondaryColor>());
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.SecondaryColor" />.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_SecondaryColor()
	{
		// Arrange
		const SecondaryColor secondaryColor = SecondaryColor.Red;

		IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

		using AutoMock mock = AutoMock.GetLoose();

		SettingsViewModel sut = mock.Create<SettingsViewModel>(TypedParameter.From(settingsManager));

		// Act
		sut.SecondaryColor = secondaryColor;

		// Assert
		sut.SecondaryColor
			.Should()
			.Be(secondaryColor);

		settingsManager.Received().SetAppMaterialTheme(
			Arg.Any<BaseThemeMode>(),
			Arg.Any<PrimaryColor>(),
			Arg.Any<SecondaryColor>());
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.IsInheritTheme" />, <see cref="SettingsViewModel.IsLightTheme" />, <see cref="SettingsViewModel.IsDarkTheme" />.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_Theme([Values] BaseThemeMode theme)
	{
		// Arrange
		IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

		using AutoMock mock = AutoMock.GetLoose();

		SettingsViewModel sut = mock.Create<SettingsViewModel>(TypedParameter.From(settingsManager));

		sut.IsInheritTheme = false;

		sut.IsLightTheme = false;

		sut.IsDarkTheme = false;

		// Act
		switch (theme)
		{
			case BaseThemeMode.Inherit:
				sut.IsInheritTheme = true;
				break;

			case BaseThemeMode.Light:
				sut.IsLightTheme = true;
				break;

			case BaseThemeMode.Dark:
				sut.IsDarkTheme = true;
				break;
		}

		// Assert
		sut.CurrentSettings.Theme
			.Should()
			.Be(theme);

		settingsManager.Received().SetAppMaterialTheme(
			Arg.Any<BaseThemeMode>(),
			Arg.Any<PrimaryColor>(),
			Arg.Any<SecondaryColor>());
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.TrackHotkeys" />.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_TrackHotkeys()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		settings.TrackHotkeys = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

			settingsManager
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsManager);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.TrackHotkeys = true;

		// Assert
		sut.CurrentSettings.TrackHotkeys
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.CurrentSettings" />.
	/// </summary>
	[Test]
	public void CurrentSettings_Initialization()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

			settingsManager
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsManager);
		});

		// Act
		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Assert
		sut.CurrentSettings.Language
			.Should()
			.Be(settings.Language);

		sut.CurrentSettings.PrimaryColor
			.Should()
			.Be(settings.PrimaryColor);

		sut.CurrentSettings.SecondaryColor
			.Should()
			.Be(settings.SecondaryColor);

		sut.CurrentSettings.Theme
			.Should()
			.Be(settings.Theme);

		sut.CurrentSettings.TrackHotkeys
			.Should()
			.Be(settings.TrackHotkeys);
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.Languages" />.
	/// </summary>
	[Test]
	public void Languages_Have_Certain_Values()
	{
		// Assert
		SettingsViewModel.Languages
			.Should()
			.Contain(IAppSettingsManager.Languages);
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.PrimaryColors" />.
	/// </summary>
	[Test]
	public void PrimaryColors_Have_Certain_Values()
	{
		// Assert
		SettingsViewModel.PrimaryColors
			.Should()
			.Contain(Enum.GetValues<PrimaryColor>());
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.SaveAndClose" />.
	/// </summary>
	[Test]
	public void SaveAndClose_Sets_Property()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.SaveAndClose();

		// Assert
		sut.IsSaved
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.SaveAndCloseCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void SaveAndCloseCommand_CanExecute_Returns_False_When_Settings_Not_Changed()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

			settingsManager
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsManager);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		bool canExecute = sut.SaveAndCloseCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.SaveAndCloseCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void SaveAndCloseCommand_CanExecute_Returns_True_After_Settings_Are_Changed()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		settings.TrackHotkeys = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

			settingsManager
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsManager);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.TrackHotkeys = true;

		bool canExecute = sut.SaveAndCloseCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="SettingsViewModel.SecondaryColors" />.
	/// </summary>
	[Test]
	public void SecondaryColors_Have_Certain_Values()
	{
		// Assert
		SettingsViewModel.SecondaryColors
			.Should()
			.Contain(Enum.GetValues<SecondaryColor>());
	}
	#endregion
}
