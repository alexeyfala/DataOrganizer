using AvaloniaEdit.Document;
using System.Linq;

namespace DataOrganizer.UI.UnitTests.TestHelpers.Factories;

/// <summary>
/// Factory methods that build text documents of a known shape.
/// </summary>
internal static class TextDocumentFactory
{
	#region Methods
	/// <summary>
	/// Creates a document of blocks, each a numbered line followed by three indented lines, which folds from the end of
	/// the numbered line to the end of the block.
	/// </summary>
	public static TextDocument CreateBlockDocument(int blockCount)
	{
		return new(string.Join('\n', Enumerable
			.Range(1, blockCount)
			.Select(static x => $"Block {x:D4}\n    One\n    Two\n    Three")));
	}
	#endregion
}
