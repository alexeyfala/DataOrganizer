using Shared.Properties;

namespace DataOrganizer.Helpers.Notes;

/// <summary>
/// Composes the header shown above a note.
/// </summary>
internal static class NoteHeaderBuilder
{
	#region Methods
	/// <summary>
	/// Composes a header for a note: the label with <paramref name="name" />, or the label alone
	/// when the name is blank.
	/// </summary>
	public static string Build(string? name)
	{
		return string.IsNullOrWhiteSpace(name)
			? Strings.Note
			: $"{Strings.Note}: {name}";
	}
	#endregion
}
