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

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsViewModel)}"" type")]
internal class SettingsViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="SettingsViewModel.Language" />: setting the language updates the current settings language.
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
	/// <see cref="SettingsViewModel.PrimaryColor" />: setting the primary color applies it and triggers a material theme update.
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
	/// <see cref="SettingsViewModel.SecondaryColor" />: setting the secondary color applies it and triggers a material theme update.
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
	/// <see cref="SettingsViewModel.IsInheritTheme" />, <see cref="SettingsViewModel.IsLightTheme" />, <see cref="SettingsViewModel.IsDarkTheme" />: selecting a theme flag sets the current settings theme and triggers a material theme update.
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
	/// <see cref="SettingsViewModel.TrackClipboardHistory" />: enabling the flag updates the current settings value.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_TrackClipboardHistory()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		settings.TrackClipboardHistory = false;

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
		sut.TrackClipboardHistory = true;

		// Assert
		sut.CurrentSettings.TrackClipboardHistory
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SettingsViewModel.TrackHotkeys" />: enabling the flag updates the current settings value.
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
	/// <see cref="SettingsViewModel.CurrentSettings" />: current settings are initialized from the settings manager values.
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

		sut.CurrentSettings.TrackClipboardHistory
			.Should()
			.Be(settings.TrackClipboardHistory);
	}

	/// <summary>
	/// <see cref="SettingsViewModel.Languages" />: the collection contains the supported languages.
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
	/// <see cref="SettingsViewModel.PrimaryColors" />: the collection contains all primary color enum values.
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
	/// <see cref="SettingsViewModel.SaveAndClose" />: invoking it sets the IsSaved property to true.
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
	/// <see cref="SettingsViewModel.SaveAndCloseCommand" /> CanExecute.
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
	/// <see cref="SettingsViewModel.SaveAndCloseCommand" /> CanExecute.
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
	/// <see cref="SettingsViewModel.SecondaryColors" />: the collection contains all secondary color enum values.
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
