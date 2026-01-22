using Cysharp.Text;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace Shared.Services;

public sealed class JsonSerializerWrapper : IJsonSerializerWrapper
{
	#region Methods
	/// <inheritdoc />
	public T? Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string json)
	{
		return JsonSerializer.Deserialize<T>(json);
	}

	/// <inheritdoc />
	public T? FromFile<T>(string filePath)
	{
		try
		{
			return Deserialize<T>(File.ReadAllText(filePath));
		}
		catch
		{
			return default;
		}
	}

	/// <inheritdoc />
	public string Serialize<T>(T value, JsonSerializerOptions? options = null)
	{
		return JsonSerializer.Serialize(value, options);
	}

	/// <inheritdoc />
	public string ToReadableJson<T>(T? target)
	{
		Type type = typeof(T);

		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		if (type.GetTypeKind() is { } typeKind)
		{
			builder.Append(typeKind);
		}

		builder.Append(' ');

		if (target is not null)
		{
			if (type.FullName is { } typeName)
			{
				builder.Append(typeName);

				builder.Append(' ');

				builder.AppendLine("to Json");
			}

			builder.Append(Serialize(target, AppUtils.JsonOptions));
		}
		else if (type.FullName is { } typeName)
		{
			builder.Append(typeName);

			builder.Append(" = null");
		}

		return builder.ToString();
	}
	#endregion
}
