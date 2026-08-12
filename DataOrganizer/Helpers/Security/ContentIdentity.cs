using DataOrganizer.Enums;
using System;

namespace DataOrganizer.Helpers.Security;

/// <summary>
/// The place a ciphertext belongs to, rendered as authenticated associated data.
/// The identifier stays out of it: an import renumbers every object.
/// </summary>
public readonly record struct ContentIdentity(Guid Id, ContentPurpose Purpose)
{
	#region Data
	/// <summary>
	/// Size of the associated data: the label and the purpose byte.
	/// </summary>
	private const int AssociatedDataSize = LabelSize + 1;

	/// <summary>
	/// Size of <see cref="Label" />.
	/// </summary>
	private const int LabelSize = 20;

	/// <summary>
	/// Domain separation label; the layout is fixed length, so the parts can never be read ambiguously.
	/// </summary>
	private static ReadOnlySpan<byte> Label => "DataOrganizer.Aad.v1"u8;
	#endregion

	#region Methods
	/// <summary>
	/// Identity of the contents of a file.
	/// </summary>
	public static ContentIdentity ForContents(Guid id) => new(id, ContentPurpose.Contents);

	/// <summary>
	/// Identity of the data encryption key of a password keeper.
	/// </summary>
	public static ContentIdentity ForDek(Guid id) => new(id, ContentPurpose.Dek);

	/// <summary>
	/// Identity of the note of a file or of a folder.
	/// </summary>
	public static ContentIdentity ForNote(Guid id) => new(id, ContentPurpose.Note);

	/// <summary>
	/// Renders the identity as the associated data of an authenticated encryption.
	/// </summary>
	public byte[] ToAssociatedData()
	{
		byte[] result = new byte[AssociatedDataSize];

		Label.CopyTo(result);

		result[LabelSize] = (byte)Purpose;

		return result;
	}
	#endregion
}
