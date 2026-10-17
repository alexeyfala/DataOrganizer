using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services.Settings;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels.Dialogs;
using Material.Colors;
using NSubstitute;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.ViewModels.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsViewModel)}"" type")]
internal class SettingsViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="SettingsViewModel.AutoLockMinutes" />: selecting a delay updates the current settings value.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_AutoLockMinutes()
	{
		// Arrange
		const int minutes = 5;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.AutoLockMinutes = minutes;

		// Assert
		sut.CurrentSettings.AutoLockMinutes
			.Should()
			.Be(minutes);
	}

	/// <summary>
	/// <see cref="SettingsViewModel.CheckForUpdates" />: toggling the flag updates the current settings value.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_CheckForUpdates()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		settings.CheckForUpdates = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
		AppSettings settings = SettingsFactory.CreateSettings();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

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
		const ThemePrimaryColor primaryColor = ThemePrimaryColor.Red;

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(themeService);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.PrimaryColor = primaryColor;

		// Assert
		sut.PrimaryColor
			.Should()
			.Be(primaryColor);

		themeService.Received().SetTheme(
			Arg.Any<ThemeMode>(),
			Arg.Any<ThemePrimaryColor>(),
			Arg.Any<ThemeSecondaryColor>());
	}

	/// <summary>
	/// <see cref="SettingsViewModel.SecondaryColor" />: setting the secondary color applies it and triggers a material theme update.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_SecondaryColor()
	{
		// Arrange
		const ThemeSecondaryColor secondaryColor = ThemeSecondaryColor.Red;

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(themeService);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.SecondaryColor = secondaryColor;

		// Assert
		sut.SecondaryColor
			.Should()
			.Be(secondaryColor);

		themeService.Received().SetTheme(
			Arg.Any<ThemeMode>(),
			Arg.Any<ThemePrimaryColor>(),
			Arg.Any<ThemeSecondaryColor>());
	}

	/// <summary>
	/// <see cref="SettingsViewModel.ShowFavoritesOnHover" />: enabling the flag updates the current settings value.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_ShowFavoritesOnHover()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		settings.ShowFavoritesOnHover = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="SettingsViewModel.IsThemeInherited" />, <see cref="SettingsViewModel.IsLightTheme" />, <see cref="SettingsViewModel.IsDarkTheme" />: selecting a theme flag sets the current settings theme and triggers a material theme update.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_Theme([Values] ThemeMode theme)
	{
		// Arrange
		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(themeService);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		sut.IsThemeInherited = false;

		sut.IsLightTheme = false;

		sut.IsDarkTheme = false;

		// Act
		switch (theme)
		{
			case ThemeMode.Inherit:
				sut.IsThemeInherited = true;
				break;

			case ThemeMode.Light:
				sut.IsLightTheme = true;
				break;

			case ThemeMode.Dark:
				sut.IsDarkTheme = true;
				break;
		}

		// Assert
		sut.CurrentSettings.Theme
			.Should()
			.Be(theme);

		themeService.Received().SetTheme(
			Arg.Any<ThemeMode>(),
			Arg.Any<ThemePrimaryColor>(),
			Arg.Any<ThemeSecondaryColor>());
	}

	/// <summary>
	/// <see cref="SettingsViewModel.TrackClipboardHistory" />: enabling the flag updates the current settings value.
	/// </summary>
	[Test]
	public void CurrentSettings_Applies_TrackClipboardHistory()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		settings.TrackClipboardHistory = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
		AppSettings settings = SettingsFactory.CreateSettings();

		settings.TrackHotkeys = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="SettingsViewModel.CurrentSettings" />: current settings are a copy of the settings manager values.
	/// </summary>
	[Test]
	public void CurrentSettings_Is_Initialized_From_The_Settings_Manager()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		settings.ShowFavoritesOnHover = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		// Act
		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Assert
		// The view edits a copy: every value is taken over, but never the stored instance itself.
		sut.CurrentSettings
			.Should()
			.Be(settings)
			.And
			.NotBeSameAs(settings);
	}

	/// <summary>
	/// <see cref="SettingsViewModel.DiscardAndClose" />: marks the settings as discarded and closes the dialog.
	/// </summary>
	[Test]
	public void DiscardAndClose_Marks_The_Settings_Discarded_And_Closes_The_Dialog()
	{
		// Arrange
		IDialogHostCloser dialogHostCloser = Substitute.For<IDialogHostCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dialogHostCloser);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.DiscardAndClose();

		// Assert
		sut.IsDiscarded
			.Should()
			.BeTrue();

		dialogHostCloser
			.Received(1)
			.Close();
	}

	/// <summary>
	/// <see cref="SettingsViewModel.PrimaryColors" />: the choice holds the primary colors of Material design, in its order.
	/// </summary>
	[Test]
	public void PrimaryColors_Lists_The_Primary_Colors_Of_Material_Design()
	{
		// Act
		string[] result = [.. SettingsViewModel.PrimaryColors.Select(x => x.ToString())];

		// Assert
		result
			.Should()
			.Equal(Enum.GetNames<PrimaryColor>());
	}

	/// <summary>
	/// <see cref="SettingsViewModel.RestoreDefaultSettingsCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void RestoreDefaultSettingsCommand_CanExecute_Returns_False_When_The_View_Already_Holds_Defaults()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings(trackHotkeys: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
		AppSettings settings = SettingsFactory.CreateSettings(trackHotkeys: true);

		settings.LastNotifiedVersion = "9.9.9";

		settings.LastUpdateCheckAt = DateTimeOffset.UnixEpoch;

		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

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

		sut.CurrentSettings
			.Should()
			.Be(defaults with
			{
				LastNotifiedVersion = settings.LastNotifiedVersion,
				LastUpdateCheckAt = settings.LastUpdateCheckAt
			});

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
	/// <see cref="SettingsViewModel.SaveAndClose" />: marks the settings as saved and closes the dialog.
	/// </summary>
	[Test]
	public void SaveAndClose_Marks_The_Settings_Saved_And_Closes_The_Dialog()
	{
		// Arrange
		IDialogHostCloser dialogHostCloser = Substitute.For<IDialogHostCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dialogHostCloser);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SettingsViewModel sut = mock.Create<SettingsViewModel>();

		// Act
		sut.SaveAndClose();

		// Assert
		sut.IsSaved
			.Should()
			.BeTrue();

		dialogHostCloser
			.Received(1)
			.Close();
	}

	/// <summary>
	/// <see cref="SettingsViewModel.SaveAndCloseCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void SaveAndCloseCommand_CanExecute_Returns_False_When_Settings_Not_Changed()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
		AppSettings settings = SettingsFactory.CreateSettings();

		settings.TrackHotkeys = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="SettingsViewModel.SecondaryColors" />: the choice holds the accent colors of Material design, in its order.
	/// </summary>
	[Test]
	public void SecondaryColors_Lists_The_Secondary_Colors_Of_Material_Design()
	{
		// Act
		string[] result = [.. SettingsViewModel.SecondaryColors.Select(x => x.ToString())];

		// Assert
		result
			.Should()
			.Equal(Enum.GetNames<SecondaryColor>());
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
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance<ISettingsSessionState>(sessionState);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
