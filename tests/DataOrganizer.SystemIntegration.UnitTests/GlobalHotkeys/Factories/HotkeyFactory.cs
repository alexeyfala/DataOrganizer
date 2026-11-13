using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.TestSupport.Common;
using SharpHook.Data;
using System;
using System.Collections.Generic;

namespace DataOrganizer.SystemIntegration.UnitTests.GlobalHotkeys.Factories;

/// <summary>
/// Factory methods that build hotkey objects filled with random values.
/// </summary>
public static class HotkeyFactory
{
	#region Methods
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
