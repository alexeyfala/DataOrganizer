using Entities.Models;
using Entities.Serialization;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataOrganizer.Services.Exchange;

/// <summary>
/// The XML format of the exchange files.
/// </summary>
public sealed class XmlExchangeFormat : SerializedExchangeFormatBase
{
	#region Data
	/// <inheritdoc cref="IXmlSerializer" />
	private readonly IXmlSerializer _xmlSerializer;
	#endregion

	#region Constructors
	public XmlExchangeFormat(
		IDbAccess dbAccess,
		IFileSystem fileSystem,
		IXmlSerializer xmlSerializer) : base(dbAccess, fileSystem)
	{
		_xmlSerializer = xmlSerializer;
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	public override string Extension => KnownFileExtensions.Xml;

	/// <inheritdoc />
	public override string FileTypeName => "XML File";

	/// <inheritdoc />
	public override string MimeType => "application/xml";
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override async Task<ExplorerItemBase[]?> ReadDocumentAsync(Stream stream, CancellationToken token)
	{
		// The document is read as a whole, so that hotkey names the library no longer knows
		// can be replaced before the serializer rejects the whole file because of them.
		XDocument document = await _xmlSerializer
			.LoadDocumentAsync(stream, token)
			.ConfigureAwait(false);

		HotkeyXmlSanitizer.Sanitize(document);

		return _xmlSerializer.Deserialize<ExplorerItemBase[]>(document);
	}

	/// <inheritdoc />
	protected override Task WriteDocumentAsync(Stream stream, ExplorerItemBase[] entities, CancellationToken token)
	{
		_xmlSerializer.Serialize(stream, entities);

		return Task.CompletedTask;
	}
	#endregion
}
