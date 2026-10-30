namespace Settings.Interfaces;

/// <summary>
/// Decides whether closing the settings asks for a confirmation first.
/// </summary>
public interface ISettingsCloseGuard
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the settings hold edits that are neither saved nor discarded.
	/// </summary>
	bool ShouldConfirmClose { get; }
	#endregion
}
