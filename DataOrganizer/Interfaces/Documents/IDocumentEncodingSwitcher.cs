using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Reads the text of an editor again from the same bytes in another encoding.
/// </summary>
public interface IDocumentEncodingSwitcher
{
	#region Methods
	/// <summary>
	/// Reads the text of an editor again in the encoding chosen by its web name, once the choice is written; <c>False</c>
	/// when the choice changes nothing or gives way, with a warning, to the encoding of the text.
	/// </summary>
	/// <param name="editor">Editor whose text is read again.</param>
	/// <param name="encoding">Web name of the chosen encoding.</param>
	/// <param name="snackbarHostIdentifier">Snackbar host of the window that shows the warnings.</param>
	/// <param name="textName">Name of the text in the log.</param>
	Task<bool> SwitchAsync(
		IEncodedDocument editor,
		string? encoding,
		string snackbarHostIdentifier,
		string textName);
	#endregion
}
