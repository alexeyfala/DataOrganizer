namespace DataOrganizer.Interfaces.Text;

/// <summary>
/// Grammars of the syntax highlighting: the language of a file and the languages that have a grammar.
/// </summary>
public interface ISyntaxRegistry
{
	#region Methods
	/// <summary>
	/// Returns the language of a file by the extension of its name; <c>null</c> when no grammar knows the extension.
	/// </summary>
	string? FindLanguage(string? fileName);

	/// <summary>
	/// Returns the scope name of the grammar of a language; <c>null</c> for a language without a grammar.
	/// </summary>
	string? FindScope(string language);
	#endregion
}
