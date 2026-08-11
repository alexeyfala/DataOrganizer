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

		try
		{
			if (FindKeeperId(item) is not { } keeperId || _sessionKeyStore.Decrypt(
				keeperId,
				ContentIdentity.ForNote(item.Id),
				note) is not { } decrypted)
			{
				return null;
			}

			try
			{
				return ToText(decrypted);
			}
			finally
			{
				decrypted.ZeroMemory();
			}
		}
		catch (CryptographicException ex)
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
			return FindKeeperId(item) is { } keeperId
				? _sessionKeyStore.Encrypt(keeperId, ContentIdentity.ForNote(item.Id), decoded)
				: null;
		}
		catch (CryptographicException ex)
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
	/// Identifier of the password keeper <paramref name="item" /> belongs to; <c>null</c> when there is no such keeper.
	/// </summary>
	/// <remarks>
	/// A password keeper protects its own note as well, hence the check of the folder itself.
	/// </remarks>
	private static Guid? FindKeeperId(ExplorerModelBaseDto item)
	{
		FolderModelDto? keeper = item is FolderModelDto folder
			? folder.FindPasswordKeeperOrSelf()
			: item.FindParent(x => x.IsPasswordKeeper());

		return keeper?.Id;
	}

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
