using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shared.Interfaces;

/// <summary>
/// Serializes and deserializes Xml through <see cref="System.Xml.Serialization" />.
/// </summary>
public interface IXmlSerializer
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
	/// Deserializes a document that has already been read into <typeparamref name="T"/>.
	/// </summary>
	T? Deserialize<T>(XDocument document);

	/// <summary>
	/// Asynchronously reads XML content from a stream into a document, with document type
	/// definitions and external resources turned off.
	/// </summary>
	Task<XDocument> LoadDocumentAsync(Stream stream, CancellationToken token = default);

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
