using System;
using System.Collections.ObjectModel;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// The settings for "Copy History".
/// </summary>
public sealed class CopyHistoryViewSettings
{
	#region Properties
	/// <summary>
	/// Identifiers of the files kept in the copy history.
	/// </summary>
	/// <remarks>
	/// Don't remove the "set;" accessor, it's required for deserialization.
	/// </remarks>
	public ObservableCollection<Guid> ItemIds { get; set; } = [];

	/// <summary>
	/// The selected item identifier in <see cref="ItemIds" />.
	/// </summary>
	public Guid SelectedItemId { get; set; }
	#endregion
}
