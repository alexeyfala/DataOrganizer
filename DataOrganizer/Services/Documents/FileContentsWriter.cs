using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Documents;

public sealed class FileContentsWriter : IFileContentsWriter
{
	#region Data
	/// <inheritdoc cref="IContentCipher" />
	private readonly IContentCipher _contentCipher;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public FileContentsWriter(
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		ILogger logger,
		TimeProvider timeProvider)
	{
		_contentCipher = contentCipher;

		_dbAccess = dbAccess;

		_logger = logger;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<FileContentsWriting> WriteAsync(
		Guid fileId,
		Guid? keeperId,
		byte[] contents,
		FileContentsChange change,
		CancellationToken token = default)
	{
		if (Encrypt(keeperId, contents) is not { } output)
		{
			return new()
			{
				Status = FileContentsWriteStatus.Closed
			};
		}

		try
		{
			DateTime? updatedAt = change == FileContentsChange.Edit
				? _timeProvider.GetLocalNow().LocalDateTime
				: null;

			if (!await _dbAccess
				.UpdateFilePropertiesAsync(fileId, CreateSetters(output, updatedAt), token)
				.ConfigureAwait(false))
			{
				return new()
				{
					Status = FileContentsWriteStatus.Missing
				};
			}

			_logger.LogDebug($@"Contents of file ""{fileId}"" are written in the database, {output.Length} bytes");

			return new()
			{
				Status = FileContentsWriteStatus.Written,
				UpdatedAt = updatedAt
			};
		}
		finally
		{
			// The ciphertext is a buffer of the writer's own, which the caller does not wipe.
			if (!ReferenceEquals(output, contents))
			{
				output.ZeroMemory();
			}
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates the setters of the contents and, when a date is given, of the date of change.
	/// </summary>
	private static Action<UpdateSettersBuilder<FileEntity>>[] CreateSetters(byte[] contents, DateTime? updatedAt)
	{
		if (updatedAt is not { } date)
		{
			return [x => x.SetProperty(x => x.Contents, contents)];
		}

		return
		[
			x => x.SetProperty(x => x.Contents, contents),
			x => x.SetProperty(x => x.UpdatedAt, date)
		];
	}

	/// <summary>
	/// Encrypts the contents with the session key of a keeper, if one is given; <c>null</c> reports a refusal.
	/// </summary>
	private byte[]? Encrypt(Guid? keeperId, byte[] contents)
	{
		// Empty content is never encrypted, as every reader takes it as it is.
		if (keeperId is not { } id || contents.IsEmpty())
		{
			return contents;
		}

		return _contentCipher.TryEncrypt(
			id,
			ContentIdentity.Contents,
			contents);
	}
	#endregion
}
