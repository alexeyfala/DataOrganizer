using DataOrganizer.Dto.Encryption;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class FolderContentsConverter : IFolderContentsConverter
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FolderContentsConverter(
		IDbAccess dbAccess,
		IEncryptionService encryption,
		ILogger logger,
		INotificationService notification)
	{
		_dbAccess = dbAccess;

		_encryption = encryption;

		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<FolderConversion?> ConvertAsync(
		FolderConversionParameters parameters,
		CancellationToken token = default)
	{
		ValidatedContents[] loaded = await _dbAccess
			.GetFileContentsRangeAsync(parameters.Files.Select(x => x.Id), token)
			.ToArrayAsync(token)
			.ConfigureAwait(false);

		ValidatedContents[] converted = [];

		NoteUpdate[] notes = [];

		bool isHandedOver = false;

		try
		{
			if (!AreContentsValid(loaded, parameters.Files.Length))
			{
				_notification.ShowErrorSnackbar(Strings.FailedToLoadFilesContents);

				return null;
			}

			converted = parameters.Encrypt
				? [.. _encryption.EncryptContents(loaded, parameters.Dek)]
				: [.. _encryption.DecryptContents(loaded, parameters.Dek)];

			if (!AreContentsValid(converted, loaded.Length))
			{
				LogInvalidContents(converted);

				_notification.ShowErrorSnackbar(parameters.Encrypt
					? Strings.FailedToProcessContents
					: Strings.EncryptedDataIsDamaged);

				return null;
			}

			notes = ProcessNotes(parameters);

			isHandedOver = true;

			return new()
			{
				Converted = converted,
				Loaded = loaded,
				Notes = notes
			};
		}
		finally
		{
			// What is not handed over has no reader left here, whichever side of it held plain text.
			if (!isHandedOver)
			{
				WipeContents(loaded);

				WipeContents(converted);

				WipeNotes(notes);
			}
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when every content is readable, carries an identifier, and there are as many of
	/// them as expected.
	/// </summary>
	private static bool AreContentsValid(ValidatedContents[] contents, int expectedCount)
	{
		return contents.Length == expectedCount
			&& contents.All(x => x.IsValid && x.Id.IsNotDefault());
	}

	/// <summary>
	/// Overwrites the buffers of the given contents.
	/// </summary>
	private static void WipeContents(ValidatedContents[] contents)
	{
		contents.ForEach(x => x.Contents.ZeroMemory());
	}

	/// <summary>
	/// Overwrites the buffers of the given notes.
	/// </summary>
	private static void WipeNotes(IEnumerable<NoteUpdate> notes) => notes.ForEach(x => x.Note.ZeroMemory());

	/// <summary>
	/// Writes the identifiers of the contents that could not be converted to the log.
	/// </summary>
	private void LogInvalidContents(ValidatedContents[] contents)
	{
		string identifiers = string.Join(", ", contents
			.Where(x => !x.IsValid)
			.Select(x => x.Id));

		_logger.LogError(
			$"The contents of these files cannot be converted: {identifiers}",
			breakInDebugger: false);
	}

	/// <summary>
	/// Converts the notes of a folder, of its subfolders and of the given files with the DEK.
	/// A note that cannot be converted throws, so the result is never partial.
	/// </summary>
	private NoteUpdate[] ProcessNotes(FolderConversionParameters parameters)
	{
		List<NoteUpdate> notes = [];

		ExplorerItemDtoBase[] objects =
		[
			.. parameters.Folder.WithSubfolders(),
			.. parameters.Files
		];

		try
		{
			foreach (ExplorerItemDtoBase item in objects)
			{
				if (item.Note is not { } note || note.IsEmpty())
				{
					continue;
				}

				ContentIdentity identity = ContentIdentity.ForNote(item.Id);

				byte[] processed = parameters.Encrypt
					? _encryption.EncryptWithDek(note, parameters.Dek, identity)
					: _encryption.DecryptWithDek(note, parameters.Dek, identity);

				notes.Add(new NoteUpdate(
					item.Id,
					item.Kind,
					processed));
			}
		}
		catch
		{
			// A partial result is thrown away, so the notes decrypted so far lose their only reader.
			if (!parameters.Encrypt)
			{
				WipeNotes(notes);
			}

			throw;
		}

		return [.. notes];
	}
	#endregion
}
