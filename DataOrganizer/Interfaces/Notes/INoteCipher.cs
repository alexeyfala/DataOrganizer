using DataOrganizer.Dto.Entities;

namespace DataOrganizer.Interfaces.Notes;

/// <summary>
/// Converts notes between their stored binary form and plain text.
/// </summary>
public interface INoteCipher
{
	#region Methods
	/// <summary>
	/// Converts the stored note of <paramref name="item" /> to plain text; <c>null</c> when there
	/// is no note or the stored bytes cannot be read.
	/// </summary>
	string? Decode(ExplorerItemDtoBase item);

	/// <summary>
	/// Converts <paramref name="note" /> to the form stored for <paramref name="item" />; <c>null</c>
	/// for blank text and when the encrypted form cannot be produced.
	/// </summary>
	byte[]? Encode(ExplorerItemDtoBase item, string? note);
	#endregion
}
