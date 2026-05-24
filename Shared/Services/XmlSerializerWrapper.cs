using Shared.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Shared.Services;

public sealed class XmlSerializerWrapper : IXmlSerializerWrapper
{
	#region Methods
	/// <inheritdoc />
	public T? Deserialize<T>([StringSyntax(StringSyntaxAttribute.Xml)] string xml)
	{
		using StringReader stringReader = new(xml);

		return DeserializeFromTextReader<T>(stringReader);
	}

	/// <inheritdoc />
	public T? Deserialize<T>(Stream stream)
	{
		XmlSerializer serializer = new(typeof(T));

		using XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null,
			Async = false
		});

		return (T?)serializer.Deserialize(xmlReader);
	}

	/// <inheritdoc />
	public string Serialize<T>(T value)
	{
		XmlSerializer serializer = new(typeof(T));

		using StringWriter writer = new();

		serializer.Serialize(writer, value);

		return writer.ToString();
	}

	/// <inheritdoc />
	public void Serialize<T>(Stream stream, T value)
	{
		XmlSerializer serializer = new(typeof(T));

		serializer.Serialize(stream, value);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Deserializes XML content from a <see cref="TextReader" /> into <typeparamref name="T"/>
	/// using a hardened <see cref="XmlReader" /> (DTD processing prohibited, no external resolver).
	/// </summary>
	private static T? DeserializeFromTextReader<T>(TextReader reader)
	{
		XmlSerializer serializer = new(typeof(T));

		using XmlReader xmlReader = XmlReader.Create(reader, new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null
		});

		return (T?)serializer.Deserialize(xmlReader);
	}
	#endregion
}
