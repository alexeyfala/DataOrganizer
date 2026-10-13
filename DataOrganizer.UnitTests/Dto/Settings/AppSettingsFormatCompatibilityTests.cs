using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Settings;
using Shared.Services;
using System;

namespace DataOrganizer.UnitTests.Dto.Settings;

[TestFixture(Description = "Guards that the application settings left by an earlier build still open")]
internal class AppSettingsFormatCompatibilityTests
{
	#region Data
	/// <summary>
	/// Application settings as an earlier build wrote them.
	/// </summary>
	private const string RecordedAppSettings = """
		{
		  "AutoLockMinutes": 15,
		  "CheckForUpdates": false,
		  "Language": "ru-RU",
		  "LastNotifiedVersion": "0.2.0",
		  "LastUpdateCheckAt": "2026-10-12T20:48:52+00:00",
		  "PersistClipboardHistory": true,
		  "PrimaryColor": 33,
		  "SecondaryColor": 167,
		  "ShowFavoritesOnHover": true,
		  "Theme": 2,
		  "TrackClipboardHistory": true,
		  "TrackHotkeys": true
		}
		""";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SystemTextJsonSerializer.Deserialize{T}(string)" />: the application settings that an earlier build wrote
	/// are read whole.
	/// </summary>
	[Test]
	public void Deserialize_Reads_The_App_Settings_Of_An_Earlier_Build()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		// Act
		AppSettings? result = sut.Deserialize<AppSettings>(RecordedAppSettings);

		// Assert
		result
			.Should()
			.BeEquivalentTo(new AppSettings
			{
				AutoLockMinutes = 15,
				CheckForUpdates = false,
				Language = "ru-RU",
				LastNotifiedVersion = "0.2.0",
				LastUpdateCheckAt = new DateTimeOffset(2026, 10, 12, 20, 48, 52, TimeSpan.Zero),
				PersistClipboardHistory = true,
				PrimaryColor = ThemePrimaryColor.Purple,
				SecondaryColor = ThemeSecondaryColor.Lime,
				ShowFavoritesOnHover = true,
				Theme = ThemeMode.Dark,
				TrackClipboardHistory = true,
				TrackHotkeys = true
			});
	}
	#endregion
}
