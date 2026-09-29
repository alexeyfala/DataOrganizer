using AvaloniaEdit.Folding;
using DataOrganizer.Helpers.Text.FoldingStrategies;

namespace DataOrganizer.Interfaces.Text;

/// <summary>
/// Way of finding the blocks that fold in a text whose lines are read at once, so that several ways share one reading.
/// </summary>
internal interface ILineFoldingStrategy
{
	#region Methods
	/// <summary>
	/// Returns the blocks of a text.
	/// </summary>
	NewFolding[] CreateNewFoldings(FoldingText text);
	#endregion
}
