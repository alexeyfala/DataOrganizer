using DataOrganizer.Helpers.Documents;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Text of an editor together with the encoding it is read and written in.
/// </summary>
public interface IEncodedDocument
{
	#region Properties
	/// <summary>
	/// Encoding the text is kept in, which reads the text from its bytes and gives its bytes back.
	/// </summary>
	DocumentCodec Codec { get; }

	/// <summary>
	/// The text.
	/// </summary>
	IDocumentText Document { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Takes from <see cref="Codec" /> the encoding the text is read and written in, and the one found from its bytes.
	/// </summary>
	void RefreshEncoding();

	/// <summary>
	/// Replaces the text with one that <see cref="Codec" /> has read, which is not an edit, so undo starts from it, and
	/// takes the encodings of the codec.
	/// </summary>
	void ReplaceText(string text);
	#endregion
}
