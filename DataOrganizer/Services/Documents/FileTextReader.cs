using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions.Text;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Documents;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Shared.Interfaces;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Documents;

/// <inheritdoc cref="IFileTextReader" />
public sealed class FileTextReader : IFileTextReader
{
	#region Data
	/// <inheritdoc cref="IEditorStateStore" />
	private readonly IEditorStateStore _editorStateStore;

	/// <inheritdoc cref="IFileContentsReader" />
	private readonly IFileContentsReader _fileContentsReader;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;
	#endregion

	#region Constructors
	public FileTextReader(
		IEditorStateStore editorStateStore,
		IFileContentsReader fileContentsReader,
		IJsonSerializer jsonSerializer)
	{
		_editorStateStore = editorStateStore;

		_fileContentsReader = fileContentsReader;

		_jsonSerializer = jsonSerializer;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<FileTextReading> ReadAsync(
		FileDto file,
		string header,
		CancellationToken token = default)
	{
		FileContentsReading reading = await _fileContentsReader
			.ReadAsync(file, header, token)
			.ConfigureAwait(false);

		return await DecodeAsync(file, reading, token).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<FileTextReading> ReadUnlockedAsync(FileDto file, CancellationToken token = default)
	{
		FileContentsReading reading = await _fileContentsReader
			.ReadUnlockedAsync(file, token)
			.ConfigureAwait(false);

		return await DecodeAsync(file, reading, token).ConfigureAwait(false);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads the text of the contents of a file once they are opened, then wipes them if they were decrypted.
	/// </summary>
	private async Task<FileTextReading> DecodeAsync(
		FileDto file,
		FileContentsReading reading,
		CancellationToken token)
	{
		if (reading.Status == FileContentsStatus.Damaged)
		{
			return new()
			{
				Status = FileTextStatus.Damaged
			};
		}

		if (reading.Status == FileContentsStatus.Closed)
		{
			return new()
			{
				Status = FileTextStatus.Closed
			};
		}

		byte[] contents = reading.Contents;

		try
		{
			string? encoding = await FindChosenEncodingAsync(file, token).ConfigureAwait(false);

			if (FileTextCodec.TryRead(contents, encoding) is not { Text: var text })
			{
				return new()
				{
					Status = FileTextStatus.NotText
				};
			}

			return new()
			{
				Status = text.Length == 0
					? FileTextStatus.Empty
					: FileTextStatus.Text,
				Text = text
			};
		}
		finally
		{
			if (file.EncryptionStatus != EncryptionStatus.None)
			{
				contents.ZeroMemory();
			}
		}
	}

	/// <summary>
	/// Returns the web name of the encoding chosen for the text of a file, from its state or, for a file not opened yet,
	/// from the stored one; <c>null</c> when the text takes the encoding found from the contents.
	/// </summary>
	private async Task<string?> FindChosenEncodingAsync(FileDto file, CancellationToken token)
	{
		// The tree carries no editor states, so a file not opened in the session has its own read on demand.
		string? json = file.EditorState ?? await _editorStateStore
			.ReadAsync(file.Id, token)
			.ConfigureAwait(false);

		if (json is null)
		{
			return null;
		}

		try
		{
			return _jsonSerializer
				.Deserialize<FileEditorState>(json)
				.Encoding;
		}
		catch (JsonException)
		{
			// A state that cannot be read holds no choice, and the editor writes a new one once the file opens.
			return null;
		}
	}
	#endregion
}
