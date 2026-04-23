namespace DataOrganizer.Abstract;

/// <summary>
/// The boolean <see cref="IsSaved" /> value.
/// </summary>
public abstract class IsSavedBase
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if the hotkeys were saved.
	/// </summary>
	public required bool IsSaved { get; init; }
	#endregion
}
