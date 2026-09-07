using Entities.Converters;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpHook.Data;

namespace Repository.Converters;

/// <summary>
/// Stores a <see cref="KeyCode" /> as its name and reads a name no longer known to the library
/// as <see cref="KeyCode.VcUndefined" />.
/// </summary>
public sealed class KeyCodeNameConverter : ValueConverter<KeyCode, string>
{
	#region Constructors
	public KeyCodeNameConverter() : base(x => x.ToString(), x => Read(x))
	{
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads a stored name, accepting only the exact text that a written name has.
	/// </summary>
	private static KeyCode Read(string value) => EnumNameReader.Read(value, KeyCode.VcUndefined);
	#endregion
}
