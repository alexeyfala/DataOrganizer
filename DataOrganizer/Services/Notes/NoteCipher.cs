using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notes;
using Shared.Extensions;

namespace DataOrganizer.Services.Notes;

public sealed class NoteCipher : INoteCipher
{
	#region Data
	/// <inheritdoc cref="IContentCipher" />
	private readonly IContentCipher _contentCipher;
	#endregion

	#region Constructors
	public NoteCipher(IContentCipher contentCipher) => _contentCipher = contentCipher;
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

		// A note is read while the interface is being rendered, so a refusal only reaches the log.
		if (_contentCipher.TryDecrypt(
			keeper.Id,
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
			// A protected note can only be written while its keeper is unlocked;
			// a failure here must not take the note editor down, the caller reports the refusal.			
			return item.EncryptionStatus == EncryptionStatus.Decrypted && item.FindPasswordKeeper() is { } keeper
				? _contentCipher.TryEncrypt(keeper.Id, ContentIdentity.ForNote(item.Id), decoded)
				: null;
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
