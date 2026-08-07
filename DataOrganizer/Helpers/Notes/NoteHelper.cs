using Shared.Properties;

namespace DataOrganizer.Helpers.Notes;

internal static class NoteHelper
{
	#region Methods
	/// <summary>
	/// Composes a header for a note: the label with <paramref name="name" />, or the label alone
	/// when the name is blank.
	/// </summary>
	public static string BuildHeader(string? name)
	{
		return string.IsNullOrWhiteSpace(name)
			? Strings.Note
			: $"{Strings.Note}: {name}";
	}
	#endregion
}
