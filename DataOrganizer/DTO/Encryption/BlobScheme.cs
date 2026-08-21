namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// A blob format together with the derivation that opens it. The pair travels as one value,
/// so a format cannot be read with the derivation of another.
/// </summary>
public readonly record struct BlobScheme
{
	#region Properties
	/// <summary>
	/// Layout of the blob.
	/// </summary>
	public required BlobFormat Format { get; init; }

	/// <summary>
	/// Derivation building the AEAD key of the format.
	/// </summary>
	public required KeyFactory KeyFactory { get; init; }
	#endregion
}
