using DataProtection.Dto;
using DataProtection.Enums;
using DataProtection.Interfaces;
using Hierarchy.Dto;
using Notifications.Interfaces;
using Repository.Dto;
using Repository.Exceptions;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TextProcessing.Extensions;

namespace DataProtection.Services;

public sealed class EncryptedContentWriter : IEncryptedContentWriter
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public EncryptedContentWriter(
		IDbAccess dbAccess,
		ILogger logger,
		INotificationService notification,
		TimeProvider timeProvider)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_notification = notification;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<UpdateDatabaseOutcome> UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		CancellationToken token = default)
	{
		try
		{
			ProtectionUpdate update = new()
			{
				Contents = parameters.Contents.ToDictionary(x => x.Id, x => x.Contents),
				EncryptedDek = parameters.EncryptedDek,
				FileNotes = parameters
					.Notes
					.Where(x => !x.IsFolderNote())
					.ToDictionary(x => x.Id, x => x.Note),
				FolderId = parameters.Folder.Id,
				FolderNotes = parameters
					.Notes
					.Where(x => x.IsFolderNote())
					.ToDictionary(x => x.Id, x => x.Note),
				UpdatedAt = _timeProvider
					.GetLocalNow()
					.LocalDateTime
			};

			await _dbAccess
				.UpdateProtectionAsync(update, token)
				.ConfigureAwait(false);

			ExplorerItemDtoBase[] objects =
			[
				.. parameters.Folder.WithSubfolders(),
				.. parameters.Files
			];

			objects.ForEach(x => x.EncryptionStatus = parameters.NewStatus);

			ApplyNotes(objects, parameters.Notes);

			parameters
				.Folder
				.EncryptedDek = parameters.EncryptedDek;

			return UpdateDatabaseOutcome.Saved;
		}
		catch (DatabaseNotWritableException ex)
		{
			// The database turned the transaction down, which is a refusal and not a bug.
			_logger.LogException(ex, breakInDebugger: false);

			return Fail(UpdateDatabaseOutcome.SaveFailed);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return Fail(UpdateDatabaseOutcome.ExceptionThrown);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Applies the processed notes to the objects, wiping the replaced buffers.
	/// </summary>
	private static void ApplyNotes(ExplorerItemDtoBase[] objects, NoteUpdate[] notes)
	{
		if (notes.Length == 0)
		{
			return;
		}

		Dictionary<Guid, byte[]> processed = notes.ToDictionary(x => x.Id, x => x.Note);

		foreach (ExplorerItemDtoBase item in objects)
		{
			if (!processed.TryGetValue(item.Id, out byte[]? note))
			{
				continue;
			}

			byte[]? replaced = item.Note;

			item.Note = note;

			// The replaced buffer holds the note in plain text after an encryption.
			replaced?.ZeroMemory();
		}
	}

	/// <summary>
	/// Tells that the contents were not written and gives <paramref name="outcome" /> back.
	/// </summary>
	private UpdateDatabaseOutcome Fail(UpdateDatabaseOutcome outcome)
	{
		_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

		return outcome;
	}
	#endregion
}
