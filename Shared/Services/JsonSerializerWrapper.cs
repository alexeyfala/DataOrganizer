using Cysharp.Text;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
	public T? Deserialize<T>(byte[] utf8Json)
	{
		return JsonSerializer.Deserialize<T>(utf8Json);
	}

	/// <inheritdoc />
	public ValueTask<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken token = default)
	{
		return JsonSerializer.DeserializeAsync<T>(utf8Json, cancellationToken: token);
	}

	/// <inheritdoc />
	public T? FromFile<T>(string filePath, ILogger? logger = null)
	{
		try
		{
			return Deserialize<T>(File.ReadAllText(filePath));
		}
		catch (Exception ex)
		{
			logger?.LogException(ex, false);

			return default;
		}
	}

	/// <inheritdoc />
	public string Serialize<T>(T value, JsonSerializerOptions? options = null)
	{
		return JsonSerializer.Serialize(value, options);
	}

	/// <inheritdoc />
	public Task SerializeAsync<T>(
		Stream utf8Json,
		T value,
		JsonSerializerOptions? options = null,
		CancellationToken token = default)
	{
		return JsonSerializer.SerializeAsync(
			utf8Json,
			value,
			options,
			token);
	}

	/// <inheritdoc />
	public byte[] SerializeToUtf8Bytes<T>(T value, JsonSerializerOptions? options = null)
	{
		return JsonSerializer.SerializeToUtf8Bytes(value, options);
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
