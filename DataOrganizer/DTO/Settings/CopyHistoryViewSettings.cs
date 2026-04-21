using System;
using System.Collections.ObjectModel;

namespace DataOrganizer.DTO.Settings;

/// <summary>
/// The settings for "Copy History".
/// </summary>
public sealed class CopyHistoryViewSettings
{
	#region Properties
	/// <summary>
	/// Copy content history.
	/// </summary>
	public ObservableCollection<Guid> Items { get; } = [];

	/// <summary>
	/// The selected item identifier in <see cref="Items" />.
	/// </summary>
	public Guid SelectedItemId { get; set; }
	#endregion
}
