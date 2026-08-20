namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// Layout of an encrypted blob: <c>[version][header][salt][nonce][ciphertext+tag]</c>,
/// where the header and the salt are present only in the formats that need them.
/// </summary>
/// <remarks>
/// Every field of the prefix is either verified against the format or an input of the key derivation,
/// which is what binds it to the ciphertext; a field that is neither has to enter the associated data.
/// </remarks>
/// <param name="Version">Byte the blob opens with.</param>
/// <param name="HeaderSize">Size of the header.</param>
/// <param name="SaltSize">Size of the salt.</param>
/// <param name="NonceSize">Size of the nonce.</param>
public readonly record struct BlobFormat(
	byte Version,
	int HeaderSize,
	int SaltSize,
	int NonceSize)
{
	#region Properties
	/// <summary>
	/// Offset of the nonce.
	/// </summary>
	public int NonceOffset => SaltOffset + SaltSize;

	/// <summary>
	/// Size of everything preceding the ciphertext.
	/// </summary>
	public int PrefixSize => NonceOffset + NonceSize;

	/// <summary>
	/// Offset of the salt.
	/// </summary>
	public int SaltOffset => HeaderOffset + HeaderSize;
	#endregion

	#region Data
	/// <summary>
	/// Offset of the header, which follows the single byte of the version.
	/// </summary>
	public const int HeaderOffset = 1;
	#endregion
}
