using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notes;
using Shared.Extensions;

namespace DataOrganizer.Services.Notes;

public sealed class NoteCipher : INoteCipher
{
	#region Data
	/// <inheritdoc cref="IEntityEncryption" />
	private readonly IEntityEncryption _entityEncryption;
	#endregion

	#region Constructors
	public NoteCipher(IEntityEncryption entityEncryption) => _entityEncryption = entityEncryption;
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

		if (FindSessionEncryptedDek(item) is not { } sessionEncryptedDek
			|| _entityEncryption.DecryptSessionContents(note, sessionEncryptedDek) is not { } decrypted)
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
			return FindSessionEncryptedDek(item) is { } sessionEncryptedDek
				? _entityEncryption.EncryptSessionContents(decoded, sessionEncryptedDek)
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
	/// Session encrypted DEK of the password keeper <paramref name="item" /> belongs to; <c>null</c>
	/// when there is no such keeper or it is locked.
	/// </summary>
	/// <remarks>
	/// A password keeper protects its own note as well, hence the check of the folder itself.
	/// </remarks>
	private static byte[]? FindSessionEncryptedDek(ExplorerModelBaseDto item)
	{
		FolderModelDto? keeper = item is FolderModelDto folder
			? folder.FindPasswordKeeperOrSelf()
			: item.FindParent(x => x.IsPasswordKeeper());

		return keeper?.SessionEncryptedDek;
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
