using Shared.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;

namespace Shared.Services;

public sealed class XmlSerializerWrapper : IXmlSerializerWrapper
{
	#region Methods
	/// <inheritdoc />
	public T? Deserialize<T>([StringSyntax(StringSyntaxAttribute.Xml)] string xml)
	{
		XmlSerializer serializer = new(typeof(T));

		using StringReader reader = new(xml);

		return (T)serializer.Deserialize(reader)!;
	}

	/// <inheritdoc />
	public string Serialize<T>(T value)
	{
		XmlSerializer serializer = new(typeof(T));

		using StringWriter writer = new();

		serializer.Serialize(writer, value);

		return writer.ToString();
	}
	#endregion
}
