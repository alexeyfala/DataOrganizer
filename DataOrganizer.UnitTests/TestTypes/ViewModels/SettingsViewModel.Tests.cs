using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services.Settings;
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
	/// <see cref="SettingsViewModel.CheckForUpdates" />: toggling the flag updates the current settings value.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_CheckForUpdates()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		settings.CheckForUpdates = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.CheckForUpdates = true;

		// Assert
		sut.CurrentSettings.CheckForUpdates
			.Should()
			.BeTrue();
	}

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

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		using AutoMock mock = AutoMock.GetLoose();

		SettingsViewModel sut = mock.Create<SettingsViewModel>(TypedParameter.From(themeService));

		// Act
		sut.PrimaryColor = primaryColor;

		// Assert
		sut.PrimaryColor
			.Should()
			.Be(primaryColor);

		themeService.Received().SetAppMaterialTheme(
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

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		using AutoMock mock = AutoMock.GetLoose();

		SettingsViewModel sut = mock.Create<SettingsViewModel>(TypedParameter.From(themeService));

		// Act
		sut.SecondaryColor = secondaryColor;

		// Assert
		sut.SecondaryColor
			.Should()
			.Be(secondaryColor);

		themeService.Received().SetAppMaterialTheme(
			Arg.Any<BaseThemeMode>(),
			Arg.Any<PrimaryColor>(),
			Arg.Any<SecondaryColor>());
	}

	/// <summary>
	/// <see cref="SettingsViewModel.ShowFavoritesOnHover" />: enabling the flag updates the current settings value.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_ShowFavoritesOnHover()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings();

		settings.ShowFavoritesOnHover = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.ShowFavoritesOnHover = true;

		// Assert
		sut.CurrentSettings.ShowFavoritesOnHover
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SettingsViewModel.IsInheritTheme" />, <see cref="SettingsViewModel.IsLightTheme" />, <see cref="SettingsViewModel.IsDarkTheme" />: selecting a theme flag sets the current settings theme and triggers a material theme update.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_Theme([Values] BaseThemeMode theme)
	{
		// Arrange
		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		using AutoMock mock = AutoMock.GetLoose();

		SettingsViewModel sut = mock.Create<SettingsViewModel>(TypedParameter.From(themeService));

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

		themeService.Received().SetAppMaterialTheme(
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
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
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
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
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

		settings.ShowFavoritesOnHover = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
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

		sut.CurrentSettings.CheckForUpdates
			.Should()
			.Be(settings.CheckForUpdates);

		sut.CurrentSettings.ShowFavoritesOnHover
			.Should()
			.Be(settings.ShowFavoritesOnHover);
	}

	/// <summary>
	/// <see cref="SettingsViewModel.RestoreDefaultSettingsCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void RestoreDefaultSettingsCommand_CanExecute_Returns_False_When_The_View_Already_Holds_Defaults()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings(trackHotkeys: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Assert
		sut.RestoreDefaultSettingsCommand
			.CanExecute(null)
			.Should()
			.BeTrue();

		// Act
		sut
			.RestoreDefaultSettingsCommand
			.Execute(null);

		// Assert
		sut.RestoreDefaultSettingsCommand
			.CanExecute(null)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SettingsViewModel.RestoreDefaultSettingsCommand" />: fills the view with the default values,
	/// keeps the update bookkeeping and leaves the result unsaved.
	/// </summary>
	[Test]
	public void RestoreDefaultSettingsCommand_Fills_Defaults_Without_Saving()
	{
		// Arrange
		AppSettings settings = TestUtils.CreateRandomSettings(trackHotkeys: true);

		settings.LastNotifiedVersion = "9.9.9";

		settings.LastUpdateCheckUtc = DateTimeOffset.UnixEpoch;

		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		settingsStore
			.Settings
			.Returns(settings);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(settingsStore));

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		AppSettings defaults = IAppSettingsStore.CreateDefaultSettings();

		// Act
		sut
			.RestoreDefaultSettingsCommand
			.Execute(null);

		// Assert
		sut.TrackHotkeys
			.Should()
			.Be(defaults.TrackHotkeys);

		sut.CurrentSettings.Theme
			.Should()
			.Be(defaults.Theme);

		sut.CurrentSettings.PrimaryColor
			.Should()
			.Be(defaults.PrimaryColor);

		sut.CurrentSettings.Language
			.Should()
			.Be(defaults.Language);

		sut.CurrentSettings.LastNotifiedVersion
			.Should()
			.Be("9.9.9");

		sut.CurrentSettings.LastUpdateCheckUtc
			.Should()
			.Be(DateTimeOffset.UnixEpoch);

		sut.IsDirty
			.Should()
			.BeTrue();

		settingsStore
			.DidNotReceive()
			.Save();

		settingsStore
			.DidNotReceive()
			.Overwrite(Arg.Any<AppSettings>());
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
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		bool canExecute = sut
			.SaveAndCloseCommand
			.CanExecute(null);

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
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.TrackHotkeys = true;

		bool canExecute = sut
			.SaveAndCloseCommand
			.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SettingsViewModel.SelectedCategoryIndex" />: starts from the category kept for the session
	/// and writes the newly selected one back to it.
	/// </summary>
	[Test]
	public void SelectedCategoryIndex_Is_Restored_From_And_Written_To_The_Session_State()
	{
		// Arrange
		SettingsSessionState sessionState = new() { LastCategoryIndex = 2 };

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(TestUtils.CreateRandomSettings());

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance<ISettingsSessionState>(sessionState);
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Assert
		sut.SelectedCategoryIndex
			.Should()
			.Be(2);

		// Act
		sut.SelectedCategoryIndex = 3;

		// Assert
		sessionState.LastCategoryIndex
			.Should()
			.Be(3);
	}
	#endregion
}
