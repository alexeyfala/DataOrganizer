using AvaloniaEdit.Folding;
using TextProcessing.Dto;

namespace DataOrganizer.Extensions;

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
