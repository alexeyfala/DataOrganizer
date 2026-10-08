namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted tab of <c>NotepadWindow</c>.
/// </summary>
/// <param name="Number">Number of the tab.</param>
/// <param name="Name">Name given to the tab, or <c>null</c>.</param>
public sealed record NotepadTabSettings(int Number, string? Name = null);
