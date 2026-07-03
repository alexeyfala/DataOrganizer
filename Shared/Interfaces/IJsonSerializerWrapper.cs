using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Interfaces;

/// <summary>
/// Wrapper over methods in <see cref="System.Text.Json" />.
/// </summary>
public interface IJsonSerializerWrapper
{
	#region Methods
	/// <summary>
	/// Deserializes a Json string into <typeparamref name="T"/>.
	/// </summary>
	T? Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string json);

	/// <summary>
	/// Deserializes UTF-8 encoded Json bytes into <typeparamref name="T"/>, avoiding the
	/// materialization of an intermediate string in memory.
	/// </summary>
	T? Deserialize<T>(byte[] utf8Json);

	/// <summary>
	/// Asynchronously deserializes Json content directly from a stream, avoiding the materialization
	/// of an intermediate string in memory.
	/// </summary>
	ValueTask<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken token = default);

	/// <summary>
	/// Deserializes a Json string into <typeparamref name="T"/> from a file.<br />
	/// Returns the default value for type <typeparamref name="T"/> if an exception occurs during deserialization.
	/// </summary>
	T? FromFile<T>(string filePath, ILogger? logger = null);

	/// <summary>
	/// Serializes data into a Json string.
	/// </summary>
	string Serialize<T>(T value, JsonSerializerOptions? options = null);

	/// <summary>
	/// Asynchronously serializes <paramref name="value"/> directly to <paramref name="utf8Json"/>,
	/// avoiding the materialization of an intermediate string in memory.
	/// </summary>
	Task SerializeAsync<T>(
		Stream utf8Json,
		T value,
		JsonSerializerOptions? options = null,
		CancellationToken token = default);

	/// <summary>
	/// Serializes data directly into UTF-8 encoded Json bytes, avoiding the materialization
	/// of an intermediate string in memory.
	/// </summary>
	byte[] SerializeToUtf8Bytes<T>(T value, JsonSerializerOptions? options = null);

	/// <summary>
	/// Returns a human-readable Json representation of the object with the type, with indentation.
	/// </summary>
	/// <remarks>
	/// The method cannot be used for serialization.
	/// </remarks>
	string ToReadableJson<T>(T? target);
	#endregion
}
