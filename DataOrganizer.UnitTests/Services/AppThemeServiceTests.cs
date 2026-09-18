using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Styling;
using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services;
using Material.Colors;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using NSubstitute;

namespace DataOrganizer.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(AppThemeService)}"" type")]
internal class AppThemeServiceTests
{
	#region Data
	/// <summary>
	/// Theme of the application as it was before a test changed it.
	/// </summary>
	private (BaseThemeMode BaseTheme, PrimaryColor Primary, SecondaryColor Secondary, ThemeVariant Variant)? _saved;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="AppThemeService.ApplyFromSettings" />: the theme held by the settings reaches the application.
	/// </summary>
	[AvaloniaTest]
	public void ApplyFromSettings_Applies_The_Theme_Of_The_Settings()
	{
		// Arrange
		AppSettings settings = IAppSettingsStore.CreateDefaultSettings();

		settings.Theme = BaseThemeMode.Light;

		settings.PrimaryColor = PrimaryColor.Purple;

		settings.SecondaryColor = SecondaryColor.Lime;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
		});

		AppThemeService sut = mock.Create<AppThemeService>(TypedParameter.From(Application.Current!));

		// Act
		sut.ApplyFromSettings();

		// Assert
		MaterialTheme theme = Application
			.Current!
			.LocateMaterialTheme<MaterialTheme>();

		theme
			.BaseTheme
			.Should()
			.Be(BaseThemeMode.Light);

		theme
			.PrimaryColor
			.Should()
			.Be(PrimaryColor.Purple);

		theme
			.SecondaryColor
			.Should()
			.Be(SecondaryColor.Lime);
	}

	/// <summary>
	/// Remembers the theme of the application, which the tests change in place.
	/// </summary>
	[SetUp]
	public void RememberTheme()
	{
		Application application = Application.Current!;

		MaterialTheme theme = application.LocateMaterialTheme<MaterialTheme>();

		_saved = (theme.BaseTheme, theme.PrimaryColor, theme.SecondaryColor, application.RequestedThemeVariant!);
	}

	/// <summary>
	/// Puts the theme of the application back, so that a change does not reach the tests that follow.
	/// </summary>
	[TearDown]
	public void RestoreTheme()
	{
		if (_saved is not { } saved)
		{
			return;
		}

		Application application = Application.Current!;

		MaterialTheme theme = application.LocateMaterialTheme<MaterialTheme>();

		theme.BaseTheme = saved.BaseTheme;

		theme.PrimaryColor = saved.Primary;

		theme.SecondaryColor = saved.Secondary;

		application.RequestedThemeVariant = saved.Variant;

		_saved = null;
	}

	/// <summary>
	/// <see cref="AppThemeService.SetTheme" />: the mode reaches both the Material theme and the
	/// requested variant of the application, and the colours reach the theme.
	/// </summary>
	[AvaloniaTest]
	public void SetTheme_Applies_The_Mode_And_The_Colors()
	{
		// Arrange
		Application application = Application.Current!;

		using AutoMock mock = AutoMock.GetLoose();

		AppThemeService sut = mock.Create<AppThemeService>(TypedParameter.From(application));

		// Act
		sut.SetTheme(
			BaseThemeMode.Dark,
			PrimaryColor.Indigo,
			SecondaryColor.Cyan);

		// Assert
		MaterialTheme theme = application.LocateMaterialTheme<MaterialTheme>();

		theme
			.BaseTheme
			.Should()
			.Be(BaseThemeMode.Dark);

		theme
			.PrimaryColor
			.Should()
			.Be(PrimaryColor.Indigo);

		theme
			.SecondaryColor
			.Should()
			.Be(SecondaryColor.Cyan);

		application
			.RequestedThemeVariant
			.Should()
			.Be(ThemeVariant.Dark);
	}
	#endregion
}
