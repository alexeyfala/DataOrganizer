using SharpHook.Data;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Entities.Converters;

/// <summary>
/// Writes a <see cref="KeyCode" /> as its name and reads a name no longer known to the library
/// as <see cref="KeyCode.VcUndefined" />.
/// </summary>
public sealed class KeyCodeJsonConverter : JsonConverter<KeyCode>
{
	#region Methods
	/// <inheritdoc />
	public override KeyCode Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		return reader.TokenType == JsonTokenType.String
			? EnumNameReader.Read(reader.GetString(), KeyCode.VcUndefined)
			: KeyCode.VcUndefined;
	}

	/// <inheritdoc />
	public override void Write(
		Utf8JsonWriter writer,
		KeyCode value,
		JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
	#endregion
}
