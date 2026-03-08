using System.Diagnostics.CodeAnalysis;

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
	/// Serializes data into an XML string.
	/// </summary>
	string Serialize<T>(T value);
	#endregion
}
