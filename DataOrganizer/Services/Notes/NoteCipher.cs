using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notes;
using Serilog;
using Shared.Extensions;
using System;
using System.Security.Cryptography;

namespace DataOrganizer.Services.Notes;

public sealed class NoteCipher : INoteCipher
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public NoteCipher(ILogger logger, ISessionKeyStore sessionKeyStore)
	{
		_logger = logger;

		_sessionKeyStore = sessionKeyStore;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public string? Decode(ExplorerModelBaseDto item)
	{
		if (item.Note is not { } note || note.IsEmpty())
		{
			return null;
		}

		if (item.EncryptionStatus == EncryptionStatus.None)
		{
			return ToText(note);
		}

		// A protected note stays unreadable while its keeper is locked, so the store is left alone.
		if (item.EncryptionStatus != EncryptionStatus.Decrypted || item.FindPasswordKeeper() is not { } keeper)
		{
			return null;
		}

		try
		{
			byte[] decrypted = _sessionKeyStore.Decrypt(
				keeper.Id,
				ContentIdentity.ForNote(item.Id),
				note);

			try
			{
				return ToText(decrypted);
			}
			finally
			{
				decrypted.ZeroMemory();
			}
		}
		catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
		{
			// A note is read while the interface is being rendered, so nothing may escape to the caller.
			_logger.LogException(ex);

			return null;
		}
	}

	/// <inheritdoc />
	public byte[]? Encode(ExplorerModelBaseDto item, string? note)
	{
		if (string.IsNullOrWhiteSpace(note))
		{
			return null;
		}

		byte[] decoded = TextHelper
			.Utf8Encoding
			.GetBytes(note);

		if (item.EncryptionStatus == EncryptionStatus.None)
		{
			return decoded;
		}

		try
		{
			// A protected note can only be written while its keeper is unlocked.
			return item.EncryptionStatus == EncryptionStatus.Decrypted && item.FindPasswordKeeper() is { } keeper
				? _sessionKeyStore.Encrypt(keeper.Id, ContentIdentity.ForNote(item.Id), decoded)
				: null;
		}
		catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
		{
			// A failure here must not take the note editor down; the caller reports the refusal.
			_logger.LogException(ex);

			return null;
		}
		finally
		{
			decoded.ZeroMemory();
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Decodes UTF-8 bytes into plain text.
	/// </summary>
	private static string ToText(byte[] note)
	{
		return TextHelper
			.Utf8Encoding
			.GetString(note);
	}
	#endregion
}
