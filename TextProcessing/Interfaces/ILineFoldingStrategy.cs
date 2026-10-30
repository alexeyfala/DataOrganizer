using TextProcessing.Dto;
using TextProcessing.Helpers;

namespace TextProcessing.Interfaces;

/// <summary>
/// Way of finding the blocks that fold in a text whose lines are read at once, so that several ways share one reading.
/// </summary>
public interface ILineFoldingStrategy
{
	#region Methods
	/// <summary>
	/// Returns the blocks of a text.
	/// </summary>
	TextFolding[] CreateNewFoldings(FoldingText text);
	#endregion
}
