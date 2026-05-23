namespace DataOrganizer.Abstract;

/// <summary>
/// The boolean <see cref="IsSaved" /> value.
/// </summary>
public abstract class IsSavedBase
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the hotkeys were saved.
	/// </summary>
	public required bool IsSaved { get; init; }
	#endregion
}
