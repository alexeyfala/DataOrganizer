namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// Layout of an encrypted blob: <c>[version][header][salt][check][nonce][ciphertext+tag]</c>,
/// where every field but the version and the nonce is present only in the formats that need it.
/// </summary>
/// <remarks>
/// The whole prefix up to the nonce enters the associated data of the encryption, and the nonce is
/// authenticated by the algorithm itself, so no field of a blob can be swapped for the field of another.
/// Every layout, and the rule that a change of one takes a new <see cref="Version" />, is written down
/// in "Docs/Encryption_Format.md".
/// </remarks>
public readonly record struct BlobFormat
{
	#region Properties
	/// <summary>
	/// Offset of the check value.
	/// </summary>
	public int CheckOffset => SaltOffset + SaltSize;

	/// <summary>
	/// Size of the value telling a wrong secret from damaged data.
	/// </summary>
	public int CheckSize { get; init; }

	/// <summary>
	/// Size of the header.
	/// </summary>
	public int HeaderSize { get; init; }

	/// <summary>
	/// Offset of the nonce.
	/// </summary>
	public int NonceOffset => CheckOffset + CheckSize;

	/// <summary>
	/// Size of the nonce.
	/// </summary>
	public required int NonceSize { get; init; }

	/// <summary>
	/// Size the plaintext of the format always has; zero when it may be any.
	/// </summary>
	public int PlaintextSize { get; init; }

	/// <summary>
	/// Size of everything preceding the ciphertext.
	/// </summary>
	public int PrefixSize => NonceOffset + NonceSize;

	/// <summary>
	/// Offset of the salt.
	/// </summary>
	public int SaltOffset => HeaderOffset + HeaderSize;

	/// <summary>
	/// Size of the salt.
	/// </summary>
	public int SaltSize { get; init; }

	/// <summary>
	/// Byte the blob opens with.
	/// </summary>
	public required byte Version { get; init; }
	#endregion

	#region Data
	/// <summary>
	/// Offset of the header, which follows the single byte of the version.
	/// </summary>
	public const int HeaderOffset = 1;
	#endregion
}
