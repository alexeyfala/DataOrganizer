using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Properties;

namespace DataOrganizer.Models.Notepad;

/// <summary>
/// Tab of the notepad.
/// </summary>
public sealed partial class NotepadTab : ObservableObject
{
	#region Properties
	/// <summary>
	/// Header: <see cref="Name" />, or <see cref="Strings.New" /> with <see cref="Number" /> while the tab has no name.
	/// </summary>
	public string Header => Name ?? $"{Strings.New} {Number}";

	/// <summary>
	/// Name given to the tab, or <c>null</c>.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Header))]
	public partial string? Name { get; set; }

	/// <summary>
	/// Number of the tab, unique among the open tabs.
	/// </summary>
	public required int Number { get; init; }
	#endregion
}
