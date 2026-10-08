namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Text read again from its stored bytes in another encoding.
/// </summary>
public sealed record EncodingChange
{
	#region Properties
	/// <summary>
	/// Name of the chosen encoding as the status bar shows it.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// The text read in the chosen encoding; <c>null</c> when the encoding cannot read the bytes.
	/// </summary>
	public required string? Text { get; init; }
	#endregion
}
