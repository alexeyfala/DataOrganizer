using DataOrganizer.Interfaces.Exchange;
using Entities.Models;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Exchange;

/// <summary>
/// The base of the formats that serialize the objects of the database into a document, which keeps no dates.
/// </summary>
public abstract class SerializedExchangeFormatBase : IExchangeFormat
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;
	#endregion

	#region Constructors
	protected SerializedExchangeFormatBase(IDbAccess dbAccess, IFileSystem fileSystem)
	{
		_dbAccess = dbAccess;

		_fileSystem = fileSystem;
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	public abstract string Extension { get; }

	/// <inheritdoc />
	public abstract string FileTypeName { get; }

	/// <inheritdoc />
	public bool IsDatabaseCopy => false;

	/// <inheritdoc />
	public abstract string MimeType { get; }
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ExportAsync(string filePath, CancellationToken token = default)
	{
		FolderEntity[] folders = await _dbAccess
			.GetAllFoldersAsync(token)
			.ConfigureAwait(false);

		FileEntity[] files = await _dbAccess
			.GetAllFilesAsync(OptionalFileProperties.Contents | OptionalFileProperties.EditorState, token)
			.ConfigureAwait(false);

		// Streaming serialization: the document goes directly to the file without being
		// materialized as a string in memory.
		await using Stream stream = _fileSystem.CreateSequentialWrite(filePath);

		await WriteDocumentAsync(stream, [.. folders.Concat<ExplorerItemBase>(files)], token).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<LoadedEntities?> ReadAsync(string filePath, CancellationToken token = default)
	{
		ExplorerItemBase[]? entities;

		await using (Stream stream = _fileSystem.OpenSequentialRead(filePath))
		{
			entities = await ReadDocumentAsync(stream, token).ConfigureAwait(false);
		}

		if (entities is null)
		{
			return null;
		}

		return new()
		{
			Files = [.. entities.OfType<FileEntity>()],
			Folders = [.. entities.OfType<FolderEntity>()]
		};
	}

	/// <summary>
	/// Reads the objects of the document in <paramref name="stream" />; <c>null</c> when it holds none.
	/// </summary>
	protected abstract Task<ExplorerItemBase[]?> ReadDocumentAsync(Stream stream, CancellationToken token);

	/// <summary>
	/// Writes <paramref name="entities" /> into <paramref name="stream" /> as a document.
	/// </summary>
	protected abstract Task WriteDocumentAsync(Stream stream, ExplorerItemBase[] entities, CancellationToken token);
	#endregion
}
