using SharpHook.Data;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Entities.Serialization;

/// <summary>
/// Writes an <see cref="EventMask" /> as its name and reads a name no longer known to the library
/// as <see cref="EventMask.None" />.
/// </summary>
public sealed class EventMaskJsonConverter : JsonConverter<EventMask>
{
	#region Methods
	/// <inheritdoc />
	public override EventMask Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		return reader.TokenType == JsonTokenType.String
			? EnumNameReader.Read(reader.GetString(), EventMask.None)
			: EventMask.None;
	}

	/// <inheritdoc />
	public override void Write(
		Utf8JsonWriter writer,
		EventMask value,
		JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
	#endregion
}
