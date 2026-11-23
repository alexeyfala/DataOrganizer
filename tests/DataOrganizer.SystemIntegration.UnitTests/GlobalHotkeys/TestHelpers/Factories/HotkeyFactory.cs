using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Factories;
using DataOrganizer.TestSupport.Common;
using SharpHook.Data;
using System;
using System.Collections.Generic;

namespace DataOrganizer.SystemIntegration.UnitTests.GlobalHotkeys.TestHelpers.Factories;

/// <summary>
/// Factory methods that build hotkey objects filled with random values.
/// </summary>
public static class HotkeyFactory
{
	#region Methods
	/// <summary>
	/// Creates a file with one hotkey of the given key and mask.
	/// </summary>
	public static FileDto CreateFileWithHotkey(KeyCode code, EventMask mask)
	{
		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.Add(new()
			{
				Code = code,
				Id = Guid.NewGuid(),
				Index = 0,
				Mask = mask,
				OwnerId = file.Id
			});

		return file;
	}

	/// <summary>
	/// Creates the required number of random <see cref="HotkeyDto" /> objects.
	/// </summary>
	public static IEnumerable<HotkeyDto> CreateHotkeyDtos(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Code = RandomValues.CreateEnumValue<KeyCode>(),
				Id = Guid.NewGuid(),
				Index = default,
				Mask = RandomValues.CreateEnumValue<EventMask>(),
				OwnerId = Guid.NewGuid()
			};
		}
	}
	#endregion
}
