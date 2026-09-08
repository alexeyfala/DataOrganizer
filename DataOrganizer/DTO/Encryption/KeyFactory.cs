using NSec.Cryptography;
using System;

namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// Produces the AEAD key of a format from its secret, its header and the per-message salt,
/// and writes the check value the format records.
/// </summary>
public delegate Key KeyFactory(
	ReadOnlySpan<byte> secret,
	ReadOnlySpan<byte> header,
	ReadOnlySpan<byte> salt,
	Span<byte> check);
