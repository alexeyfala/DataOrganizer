using AvaloniaEdit.Folding;
using DataOrganizer.Documents.TextProcessing.Dto;

namespace DataOrganizer.Extensions;

/// <summary>
/// Converts the folded blocks of a text to the blocks of the editor engine.
/// </summary>
internal static class TextFoldingExtensions
{
	#region Methods
	/// <summary>
	/// Returns the block of the engine with the bounds and the name of a block of a text.
	/// </summary>
	public static NewFolding ToNewFolding(this TextFolding folding)
	{
		return new NewFolding(folding.StartOffset, folding.EndOffset)
		{
			Name = folding.Name
		};
	}
	#endregion
}
