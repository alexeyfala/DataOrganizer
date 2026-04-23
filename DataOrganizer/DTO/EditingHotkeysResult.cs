using Repository.DTO;

namespace DataOrganizer.DTO;

/// <summary>
/// Result of editing hotkeys.
/// </summary>
public sealed class EditingHotkeysResult
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if the hotkeys were saved.
	/// </summary>
	public required bool IsSaved { get; init; }

	/// <summary>
	/// New hotkeys.
	/// </summary>
	public required CodeMaskPair[] NewHotkeys { get; init; }
	#endregion
}
