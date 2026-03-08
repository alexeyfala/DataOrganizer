using Shared.Interfaces;
using System.IO;
using System.Xml.Serialization;

namespace Shared.Services;

public sealed class XmlSerializerWrapper : IXmlSerializerWrapper
{
	#region Methods
	/// <inheritdoc />
	public string Serialize<T>(T value) where T : notnull
	{
		XmlSerializer serializer = new(typeof(T));

		using StringWriter writer = new();

		serializer.Serialize(writer, value);

		return writer.ToString();
	}
	#endregion
}
