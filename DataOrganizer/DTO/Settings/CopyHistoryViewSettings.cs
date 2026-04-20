using System;
using System.Collections.Generic;

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
	public List<Guid> Items { get; set; } = [];

	/// <summary>
	/// The selected item identifier in <see cref="Items" />.
	/// </summary>
	public Guid SelectedItemId { get; set; }
	#endregion
}
