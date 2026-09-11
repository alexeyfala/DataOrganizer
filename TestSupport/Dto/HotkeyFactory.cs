using DataOrganizer.Dto.Entities;
using Repository.Dto;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using TestSupport.Common;

namespace TestSupport.Dto;

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

	/// <summary>
	/// Creates the required number of random <see cref="KeyStroke" /> objects.
	/// </summary>
	public static IEnumerable<KeyStroke> CreateKeyStrokes(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Code = RandomValues.CreateEnumValue<KeyCode>(),
				Mask = RandomValues.CreateEnumValue<EventMask>()
			};
		}
	}
	#endregion
}
