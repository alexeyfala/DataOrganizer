namespace DataOrganizer.Dto.Clipboard;

/// <summary>
/// A contiguous run of text flagged as a query match or as plain text.
/// </summary>
public readonly record struct SearchHighlightSegment(string Text, bool IsMatch);
