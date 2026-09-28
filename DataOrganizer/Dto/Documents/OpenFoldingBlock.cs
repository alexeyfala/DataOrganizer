namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Block of a text that is still open while the lines are read upwards to find the blocks that fold.
/// </summary>
/// <param name="Indent">Indentation of the lines of the block; lower than that of any line for an end marker.</param>
/// <param name="EndAbove">Line before which the block of a less indented line above ends.</param>
/// <param name="Line">Line of the end marker of a marked block.</param>
public readonly record struct OpenFoldingBlock(int Indent, int EndAbove, int Line);
