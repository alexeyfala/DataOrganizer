namespace DataOrganizer.DTO;

/// <summary>
/// The pair of string <see cref="Key" /> and <see cref="Value" />.
/// </summary>
public sealed class KeyValuePair
{
	#region Properties
	/// <summary>
	/// Key.
	/// </summary>
	public required string Key { get; init; }

	/// <summary>
	/// Value.
	/// </summary>
	public string? Value { get; init; }
	#endregion
}
