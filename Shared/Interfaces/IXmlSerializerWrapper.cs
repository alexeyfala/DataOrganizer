namespace Shared.Interfaces;

/// <summary>
/// Wrapper over methods in <see cref="System.Xml.Serialization" />.
/// </summary>
public interface IXmlSerializerWrapper
{
	#region Methods
	/// <summary>
	/// Serializes data into an XML string.
	/// </summary>
	string Serialize<T>(T value) where T : notnull;
	#endregion
}
