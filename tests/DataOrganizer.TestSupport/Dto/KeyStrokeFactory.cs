using DataOrganizer.Database.Repository.Dto;
using DataOrganizer.TestSupport.Common;
using SharpHook.Data;
using System.Collections.Generic;

namespace DataOrganizer.TestSupport.Dto;

/// <summary>
/// Factory methods that build key stroke objects filled with random values.
/// </summary>
public static class KeyStrokeFactory
{
	#region Methods
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
