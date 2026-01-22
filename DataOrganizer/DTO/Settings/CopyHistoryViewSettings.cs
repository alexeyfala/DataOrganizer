using System;
using System.Collections.Generic;

namespace DataOrganizer.DTO.Settings;

/// <summary>
/// The settings for "Copy History" view.
/// </summary>
public sealed class CopyHistoryViewSettings
{
	#region Properties
	/// <summary>
	/// Copy content history.
	/// </summary>
	public List<Guid> CopyHistory { get; set; } = [];

	/// <summary>
	/// The selected item identifier in <see cref="CopyHistory" />.
	/// </summary>
	public Guid SelectedCopyHistoryItemId { get; set; }
	#endregion
}
