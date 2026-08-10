using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notes;
using Shared.Extensions;
using System;

namespace DataOrganizer.Services.Notes;

public sealed class NoteCipher : INoteCipher
{
	#region Data
	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public NoteCipher(ISessionKeyStore sessionKeyStore) => _sessionKeyStore = sessionKeyStore;
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

		if (FindKeeperId(item) is not { } keeperId || _sessionKeyStore.Decrypt(keeperId, note) is not { } decrypted)
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
			return FindKeeperId(item) is { } keeperId
				? _sessionKeyStore.Encrypt(keeperId, decoded)
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
