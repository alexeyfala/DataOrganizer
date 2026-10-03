using AvaloniaEdit.Folding;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using DataOrganizer.Interfaces.Text;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Fakes;

/// <summary>
/// Test-only <see cref="ILineFoldingStrategy" /> that returns the blocks it was given for any text and keeps the texts
/// it read, making the join of several ways observable in unit-test assertions.
/// </summary>
internal sealed class FixedFoldingStrategy : ILineFoldingStrategy
{
	#region Properties
	/// <summary>
	/// Texts it was asked for blocks, in the order of the calls.
	/// </summary>
	public List<FoldingText> Texts { get; } = [];
	#endregion

	#region Data
	/// <summary>
	/// Blocks it returns for any text.
	/// </summary>
	private readonly NewFolding[] _blocks;
	#endregion

	#region Constructors
	public FixedFoldingStrategy(params NewFolding[] blocks)
	{
		_blocks = blocks;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public NewFolding[] CreateNewFoldings(FoldingText text)
	{
		Texts.Add(text);

		return _blocks;
	}
	#endregion
}
