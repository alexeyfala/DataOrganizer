namespace DialogContracts.Dto;

/// <summary>
/// A type of files that a file dialog offers to choose.
/// </summary>
public sealed class FileTypeFilter
{
	#region Properties
	/// <summary>
	/// Media types of the files; <c>null</c> when the type has none.
	/// </summary>
	public required string[]? MimeTypes { get; init; }

	/// <summary>
	/// Name of the type shown in the dialog.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Patterns of the names of the files, such as <c>*.json</c>.
	/// </summary>
	public required string[] Patterns { get; init; }
	#endregion
}
