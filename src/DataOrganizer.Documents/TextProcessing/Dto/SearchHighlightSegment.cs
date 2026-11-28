namespace DataOrganizer.Documents.TextProcessing.Dto;

/// <summary>
/// A contiguous run of text flagged as a query match or as plain text.
/// </summary>
/// <param name="Text">Text of the run.</param>
/// <param name="IsMatch">Whether the run matches the query.</param>
public readonly record struct SearchHighlightSegment(string Text, bool IsMatch);
