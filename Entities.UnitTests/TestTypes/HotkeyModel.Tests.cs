using AwesomeAssertions;
using Entities.Models;
using SharpHook.Data;
using System;
using System.Text.Json;

namespace Entities.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeyModel)}"" type")]
internal class HotkeyModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeyModel" />: names written to Json are read back as the same hotkey.
	/// </summary>
	[Test]
	public void Reads_Enums_By_Name()
	{
		// Arrange
		HotkeyModel hotkey = CreateHotkey(KeyCode.VcA, EventMask.LeftCtrl | EventMask.LeftShift);

		// Act
		HotkeyModel? result = JsonSerializer.Deserialize<HotkeyModel>(JsonSerializer.Serialize(hotkey));

		// Assert
		result
			.Should()
			.NotBeNull();

		result!
			.Code
			.Should()
			.Be(hotkey.Code);

		result
			.Mask
			.Should()
			.Be(hotkey.Mask);
	}

	/// <summary>
	/// <see cref="HotkeyModel" />: names the library no longer has do not break reading the hotkey.
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
		HotkeyModel? result = JsonSerializer.Deserialize<HotkeyModel>(json);

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
	/// <see cref="HotkeyModel" />: a hotkey is written to Json by name, not by number.
	/// </summary>
	[Test]
	public void Writes_Enums_As_Names()
	{
		// Arrange
		HotkeyModel hotkey = CreateHotkey(KeyCode.VcA, EventMask.LeftCtrl | EventMask.LeftShift);

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
	private static HotkeyModel CreateHotkey(KeyCode code, EventMask mask)
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
