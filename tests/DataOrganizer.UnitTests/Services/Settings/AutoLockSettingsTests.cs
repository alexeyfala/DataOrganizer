using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Infrastructure.Settings.Dto;
using DataOrganizer.Infrastructure.Settings.Interfaces;
using DataOrganizer.Infrastructure.UnitTests.Settings.Factories;
using DataOrganizer.Services.Settings;
using NSubstitute;

namespace DataOrganizer.UnitTests.Services.Settings;

[TestFixture(Description = $@"Tests of ""{nameof(AutoLockSettings)}"" type")]
internal class AutoLockSettingsTests
{
	#region Methods
	/// <summary>
	/// <see cref="AutoLockSettings.AutoLockMinutes" />: the delay comes from the settings at each read, so a delay
	/// changed after the start counts.
	/// </summary>
	[Test]
	public void AutoLockMinutes_Reads_The_Delay_Changed_In_The_Settings()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings(autoLockMinutes: 5);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);
		});

		AutoLockSettings sut = mock.Create<AutoLockSettings>();

		settings.AutoLockMinutes = 15;

		// Act
		int result = sut.AutoLockMinutes;

		// Assert
		result
			.Should()
			.Be(15);
	}
	#endregion
}
