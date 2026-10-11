using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Documents;

public sealed class FileTextReader : IFileTextReader
{
	#region Data
	/// <inheritdoc cref="IContentCipher" />
	private readonly IContentCipher _contentCipher;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public FileTextReader(
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IJsonSerializer jsonSerializer,
		ILogger logger)
	{
		_contentCipher = contentCipher;

		_dbAccess = dbAccess;

		_jsonSerializer = jsonSerializer;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<FileTextReading> ReadAsync(
		FileDto file,
		string header,
		CancellationToken token = default)
	{
		ValidatedContents result = await _dbAccess
			.GetFileContentsAsync(file.Id, token)
			.ConfigureAwait(false);

		if (!result.IsValid)
		{
			return new()
			{
				Status = FileTextStatus.Damaged
			};
		}

		if (await _contentCipher
			.TryDecryptContentsAsync(file, result.Contents, header, token)
			.ConfigureAwait(false) is not { } contents)
		{
			return new()
			{
				Status = FileTextStatus.Closed
			};
		}

		return Decode(file, contents);
	}

	/// <inheritdoc />
	public async Task<FileTextReading> ReadUnlockedAsync(FileDto file, CancellationToken token = default)
	{
		ValidatedContents result = await _dbAccess
			.GetFileContentsAsync(file.Id, token)
			.ConfigureAwait(false);

		if (!result.IsValid)
		{
			return new()
			{
				Status = FileTextStatus.Damaged
			};
		}

		if (file.EncryptionStatus == EncryptionStatus.Encrypted)
		{
			return new()
			{
				Status = FileTextStatus.Closed
			};
		}

		byte[] contents = result.Contents;

		if (file.EncryptionStatus == EncryptionStatus.Decrypted)
		{
			try
			{
				contents = _contentCipher.Decrypt(file, contents);
			}
			catch (Exception ex) when (EncryptionFailures.IsSessionCipher(ex))
			{
				// The text is read without a prompt, so the failure only reaches the log.
				_logger.LogException(ex);

				return new()
				{
					Status = FileTextStatus.Closed
				};
			}
		}

		return Decode(file, contents);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads the text of the opened contents of a file, then wipes them if they were decrypted.
	/// </summary>
	private FileTextReading Decode(FileDto file, byte[] contents)
	{
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
