using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using Material.Colors;
using Material.Styles.Themes.Base;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(AppThemeService)}"" type")]
internal class AppThemeServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppThemeService.ApplyMaterialTheme" />: does not throw when running under NUnit.
	/// </summary>
	[Test]
	public void ApplyMaterialTheme_Does_Not_Throw_When_Running_Under_NUnit()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(CreateStore(IAppSettingsStore.CreateDefaultSettings())));

		AppThemeService sut = mock.Create<AppThemeService>();

		// Act
		Action act = sut.ApplyMaterialTheme;

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="AppThemeService.SetAppMaterialTheme" />: is a no-op and does not throw when running under NUnit.
	/// </summary>
	[Test]
	public void SetAppMaterialTheme_Is_NoOp_When_Running_Under_NUnit()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		AppThemeService sut = mock.Create<AppThemeService>();

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
