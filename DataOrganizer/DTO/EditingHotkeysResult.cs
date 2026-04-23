using DataOrganizer.Abstract;
using Repository.DTO;

namespace DataOrganizer.DTO;

/// <summary>
/// Result of editing hotkeys.
/// </summary>
public sealed class EditingHotkeysResult : IsSavedBase
{
	#region Properties
	/// <summary>
	/// New hotkeys.
	/// </summary>
	public required CodeMaskPair[] NewHotkeys { get; init; }
	#endregion
}
