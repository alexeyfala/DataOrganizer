namespace DataOrganizer.Dto.Dialogs;

/// <summary>
/// What a dialog that chooses files to open offers.
/// </summary>
public sealed class FileOpenParameters
{
	#region Properties
	/// <summary>
	/// <c>True</c> when more than one file can be chosen.
	/// </summary>
	public required bool AllowMultiple { get; init; }

	/// <summary>
	/// Types of the files to choose among, in the order the dialog lists them.
	/// </summary>
	public required FileTypeFilter[] FileTypes { get; init; }

	/// <summary>
	/// Title of the dialog.
	/// </summary>
	public required string Title { get; init; }
	#endregion
}
