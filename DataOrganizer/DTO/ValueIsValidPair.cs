using DataOrganizer.Abstract;

namespace DataOrganizer.DTO;

/// <summary>
/// The pair of <see cref="IsValid" /> and <see cref="StringValue.Value" />.
/// </summary>
public sealed class ValueIsValidPair : StringValue
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if <see cref="StringValue.Value" /> is valid.
	/// </summary>
	public bool IsValid { get; init; }
	#endregion
}
