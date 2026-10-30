namespace DialogContracts.Dto;

/// <summary>
/// What a dialog that chooses a file to save offers.
/// </summary>
public sealed class FileSaveParameters
{
	#region Properties
	/// <summary>
	/// Extension given to a name typed without one, without the dot.
	/// </summary>
	public required string DefaultExtension { get; init; }

	/// <summary>
	/// Types of the file to choose among, in the order the dialog lists them.
	/// </summary>
	public required FileTypeFilter[] FileTypes { get; init; }

	/// <summary>
	/// <c>True</c> when the dialog asks before overwriting a file.
	/// </summary>
	public required bool ShowOverwritePrompt { get; init; }

	/// <summary>
	/// Name of the file the dialog suggests.
	/// </summary>
	public required string SuggestedFileName { get; init; }

	/// <summary>
	/// Title of the dialog.
	/// </summary>
	public required string Title { get; init; }
	#endregion
}
