using Repository.Dto;
using SharpHook.Data;
using System.Collections.Generic;
using TestSupport.Common;

namespace TestSupport.Dto;

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
