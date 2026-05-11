using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

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
	/// Deserializes a Json string into <typeparamref name="T"/> from a file.<br />
	/// Returns the default value for type <typeparamref name="T"/> if an exception occurs during deserialization.
	/// </summary>
	T? FromFile<T>(string filePath);

	/// <summary>
	/// Injects <see cref="ILogger" /> dependency.
	/// </summary>
	void InjectDependency(ILogger logger);

	/// <summary>
	/// Serializes data into a Json string.
	/// </summary>
	string Serialize<T>(T value, JsonSerializerOptions? options = null);

	/// <summary>
	/// Returns a human-readable Json representation of the object with the type, with indentation.
	/// </summary>
	/// <remarks>
	/// The method cannot be used for serialization.
	/// </remarks>
	string ToReadableJson<T>(T? target);
	#endregion
}
