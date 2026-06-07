namespace DataOrganizer.DTO;

/// <summary>
/// The pair of <see cref="IsValid" /> and <see cref="Value" />.
/// </summary>
public sealed class ValueIsValidPair
{
	#region Properties
	/// <summary>
	/// <c>True</c> when <see cref="Value" /> is valid.
	/// </summary>
	public bool IsValid { get; init; }

	/// <summary>
	/// Value.
	/// </summary>
	public string? Value { get; init; }
	#endregion
}
