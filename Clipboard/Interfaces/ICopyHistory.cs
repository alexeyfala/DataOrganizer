using System;
using System.Collections.ObjectModel;

namespace Clipboard.Interfaces;

/// <summary>
/// The files whose contents were copied, the latest first, with the one chosen in their list.
/// </summary>
public interface ICopyHistory
{
	#region Properties
	/// <summary>
	/// Identifiers of the copied files, the latest first.
	/// </summary>
	ReadOnlyObservableCollection<Guid> ItemIds { get; }

	/// <summary>
	/// The identifier of the file chosen in the list of the history.
	/// </summary>
	Guid SelectedItemId { get; set; }
	#endregion
}
