using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpHook.Data;
using System;

namespace Repository.Converters;

/// <summary>
/// Stores an <see cref="EventMask" /> as its name and reads a name no longer known to the library
/// as <see cref="EventMask.None" />.
/// </summary>
public sealed class EventMaskNameConverter : ValueConverter<EventMask, string>
{
	#region Constructors
	public EventMaskNameConverter() : base(x => x.ToString(), x => Read(x))
	{
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads a stored name, accepting only the exact text that a written name has.
	/// </summary>
	private static EventMask Read(string value)
	{
		return Enum.TryParse(value, out EventMask mask) && string.Equals(mask.ToString(), value, StringComparison.Ordinal)
			? mask
			: EventMask.None;
	}
	#endregion
}
