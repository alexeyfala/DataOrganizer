using FileContents.Enums;

namespace FileContents.Dto;

/// <summary>
/// Outcome of reading the stored contents of a file.
/// </summary>
public sealed record FileContentsReading
{
	#region Properties
	/// <summary>
	/// Contents of the file, decrypted when protected, for the caller to wipe; empty unless <see cref="Status" /> is
	/// <see cref="FileContentsStatus.Opened" />.
	/// </summary>
	public byte[] Contents { get; init; } = [];

	/// <inheritdoc cref="FileContentsStatus" />
	public required FileContentsStatus Status { get; init; }
	#endregion
}
