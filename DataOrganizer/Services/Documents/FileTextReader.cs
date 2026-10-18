using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions.Text;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Documents;
using Shared.Interfaces;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Documents;

public sealed class FileTextReader : IFileTextReader
{
	#region Data
	/// <inheritdoc cref="IFileContentsReader" />
	private readonly IFileContentsReader _fileContentsReader;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;
	#endregion

	#region Constructors
	public FileTextReader(IFileContentsReader fileContentsReader, IJsonSerializer jsonSerializer)
	{
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

		return Decode(file, reading);
	}

	/// <inheritdoc />
	public async Task<FileTextReading> ReadUnlockedAsync(FileDto file, CancellationToken token = default)
	{
		FileContentsReading reading = await _fileContentsReader
			.ReadUnlockedAsync(file, token)
			.ConfigureAwait(false);

		return Decode(file, reading);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads the text of the contents of a file once they are opened, then wipes them if they were decrypted.
	/// </summary>
	private FileTextReading Decode(FileDto file, FileContentsReading reading)
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
			if (FileTextCodec.TryRead(contents, FindChosenEncoding(file)) is not { Text: var text })
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
	/// Returns the web name of the encoding chosen for the text of a file; <c>null</c> when the text takes the one found
	/// from the contents.
	/// </summary>
	private string? FindChosenEncoding(FileDto file)
	{
		if (file.EditorState is not { } json)
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
