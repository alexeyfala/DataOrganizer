namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Text of a file read from its bytes, together with the encoding it is read in.
/// </summary>
public sealed record FileText
{
	#region Properties
	/// <summary>
	/// Encoding the text is read in, which writes it back.
	/// </summary>
	public required FileEncoding Encoding { get; init; }

	/// <summary>
	/// Text of the file without the byte order mark.
	/// </summary>
	public required string Text { get; init; }
	#endregion
}
