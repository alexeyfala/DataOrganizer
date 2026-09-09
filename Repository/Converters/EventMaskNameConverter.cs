using Entities.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpHook.Data;

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
	private static EventMask Read(string value) => EnumNameReader.Read(value, EventMask.None);
	#endregion
}
