using Entities.Models;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchyExchange.Services;

/// <summary>
/// The JSON format of the exchange files.
/// </summary>
public sealed class JsonExchangeFormat : SerializedExchangeFormatBase
{
	#region Data
	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;
	#endregion

	#region Constructors
	public JsonExchangeFormat(
		IDbAccess dbAccess,
		IFileSystem fileSystem,
		IJsonSerializer jsonSerializer) : base(dbAccess, fileSystem)
	{
		_jsonSerializer = jsonSerializer;
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	public override string Extension => KnownFileExtensions.Json;

	/// <inheritdoc />
	public override string FileTypeName => "JSON File";

	/// <inheritdoc />
	public override string MimeType => "application/json";
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override Task<ExplorerItemBase[]?> ReadDocumentAsync(Stream stream, CancellationToken token)
	{
		// Streaming deserialization: avoids loading the entire file into a string before parsing.
		return _jsonSerializer
			.DeserializeAsync<ExplorerItemBase[]>(stream, token)
			.AsTask();
	}

	/// <inheritdoc />
	protected override Task WriteDocumentAsync(Stream stream, ExplorerItemBase[] entities, CancellationToken token)
	{
		return _jsonSerializer.SerializeAsync(stream, entities, JsonDefaults.Options, token);
	}
	#endregion
}
