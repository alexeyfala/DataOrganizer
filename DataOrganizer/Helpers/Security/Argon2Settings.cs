using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DataOrganizer.Helpers.Security;

/// <summary>
/// Cost of the password based key derivation, recorded within the blob it produced,
/// so a later change of the cost leaves earlier blobs readable.
/// </summary>
/// <param name="MemorySize">Memory of a single derivation, in kibibytes.</param>
/// <param name="NumberOfPasses">Number of passes over that memory.</param>
/// <param name="DegreeOfParallelism">Number of lanes the derivation runs in.</param>
public readonly record struct Argon2Settings(
	int MemorySize,
	int NumberOfPasses,
	int DegreeOfParallelism)
{
	#region Properties
	/// <summary>
	/// Cost every new blob is written with: the interactive level of libsodium.
	/// </summary>
	public static Argon2Settings Current => new(
		MemorySize: 65536,
		NumberOfPasses: 3,
		DegreeOfParallelism: 1);
	#endregion

	#region Data
	/// <summary>
	/// Size of the layout the values occupy: <c>[memory:4][passes:1][parallelism:1]</c>.
	/// </summary>
	public const int HeaderSize = 6;

	/// <summary>
	/// Upper bound of <see cref="DegreeOfParallelism" />.
	/// </summary>
	private const int MaxDegreeOfParallelism = 4;

	/// <summary>
	/// Upper bound of <see cref="MemorySize" />: a gibibyte, past which a derivation
	/// costs more than any machine running the application can spare.
	/// </summary>
	private const int MaxMemorySize = 1048576;

	/// <summary>
	/// Upper bound of <see cref="NumberOfPasses" />.
	/// </summary>
	private const int MaxNumberOfPasses = 16;

	/// <summary>
	/// Lower bound of <see cref="MemorySize" />: eight mebibytes.
	/// </summary>
	private const int MinMemorySize = 8192;
	#endregion

	#region Methods
	/// <summary>
	/// Reads the values a blob was written with.
	/// </summary>
	/// <exception cref="CryptographicException">The layout is too short or holds unsupported values.</exception>
	public static Argon2Settings Read(ReadOnlySpan<byte> header)
	{
		if (header.Length < HeaderSize)
		{
			throw new CryptographicException(
				$"A key derivation layout of {header.Length} bytes is too short.");
		}

		Argon2Settings result = new(
			MemorySize: (int)Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(header), int.MaxValue),
			NumberOfPasses: header[4],
			DegreeOfParallelism: header[5]);

		// The values steer an allocation, so they are bounded before they reach the algorithm.
		if (result.MemorySize is < MinMemorySize or > MaxMemorySize
			|| result.NumberOfPasses is < 1 or > MaxNumberOfPasses
			|| result.DegreeOfParallelism is < 1 or > MaxDegreeOfParallelism)
		{
			throw new CryptographicException(
				$"The key derivation cost {result} is not supported.");
		}

		return result;
	}

	/// <summary>
	/// Writes the values into the layout of a blob.
	/// </summary>
	public void Write(Span<byte> header)
	{
		BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)MemorySize);

		header[4] = (byte)NumberOfPasses;

		header[5] = (byte)DegreeOfParallelism;
	}
	#endregion
}
