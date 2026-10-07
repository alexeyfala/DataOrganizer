using Shared.Properties;

namespace DataOrganizer.Models.Notepad;

/// <summary>
/// Tab of the notepad.
/// </summary>
public sealed class NotepadTab
{
	#region Properties
	/// <summary>
	/// Header: <see cref="Strings.New" /> with <see cref="Number" />.
	/// </summary>
	public string Header => $"{Strings.New} {Number}";

	/// <summary>
	/// Number of the tab, unique among the open tabs.
	/// </summary>
	public required int Number { get; init; }
	#endregion
}
