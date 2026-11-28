using AwesomeAssertions;
using DataOrganizer.Database.Entities.Models;
using SharpHook.Data;
using System;
using System.Text.Json;

namespace DataOrganizer.Database.UnitTests.Entities.Models;

/// <summary>
/// Tests of <see cref="HotkeyEntity" /> type.
/// </summary>
[TestFixture]
internal sealed class HotkeyEntityTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeyEntity" />: names the library no longer has do not break reading the hotkey.
	/// </summary>
	[Test]
	public void Reads_Unknown_Names_As_Fallbacks()
	{
		// Arrange
		string json = JsonSerializer
			.Serialize(CreateHotkey(KeyCode.VcA, EventMask.LeftCtrl))
			.Replace(@"""VcA""", @"""VcKanji""", StringComparison.Ordinal)
			.Replace(@"""LeftCtrl""", @"""LeftHyper""", StringComparison.Ordinal);

		// Act
		HotkeyEntity? result = JsonSerializer.Deserialize<HotkeyEntity>(json);

		// Assert
		result
			.Should()
			.NotBeNull();

		result!
			.Code
			.Should()
			.Be(KeyCode.VcUndefined);

		result
			.Mask
			.Should()
			.Be(EventMask.None);
	}

	/// <summary>
	/// <see cref="HotkeyEntity" />: a hotkey is written to Json by name, not by number.
	/// </summary>
	[Test]
	public void Writes_Enums_As_Names()
	{
		// Arrange
		HotkeyEntity hotkey = CreateHotkey(KeyCode.VcA, EventMask.LeftCtrl | EventMask.LeftShift);

		// Act
		string result = JsonSerializer.Serialize(hotkey);

		// Assert
		result
			.Should()
			.Contain(@"""Code"":""VcA""")
			.And
			.Contain(@"""Mask"":""LeftShift, LeftCtrl""");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a hotkey with the given key and mask.
	/// </summary>
	private static HotkeyEntity CreateHotkey(KeyCode code, EventMask mask)
	{
		return new()
		{
			Code = code,
			Id = Guid.NewGuid(),
			Index = 0,
			Mask = mask,
			OwnerId = Guid.NewGuid()
		};
	}
	#endregion
}
