using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Shared.Interfaces;

/// <summary>
/// Wrapper over methods in <see cref="System.Xml.Serialization" />.
/// </summary>
public interface IXmlSerializerWrapper
{
	#region Methods
	/// <summary>
	/// Deserializes a XML string into <typeparamref name="T"/>.
	/// </summary>
	T? Deserialize<T>([StringSyntax(StringSyntaxAttribute.Xml)] string xml);

	/// <summary>
	/// Deserializes XML content directly from a stream into <typeparamref name="T"/>,
	/// avoiding the materialization of an intermediate string in memory.
	/// </summary>
	T? Deserialize<T>(Stream stream);

	/// <summary>
	/// Serializes data into an XML string.
	/// </summary>
	string Serialize<T>(T value);

	/// <summary>
	/// Serializes <paramref name="value"/> directly into <paramref name="stream"/>,
	/// avoiding the materialization of an intermediate string in memory.
	/// </summary>
	void Serialize<T>(Stream stream, T value);
	#endregion
}
