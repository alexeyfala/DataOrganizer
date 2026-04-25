using DataOrganizer.Abstract;

namespace DataOrganizer.DTO;

/// <summary>
/// The pair of <see cref="IsValid" /> and <see cref="StringValueBase.Value" />.
/// </summary>
public sealed class ValueIsValidPair : StringValueBase
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if <see cref="StringValueBase.Value" /> is valid.
	/// </summary>
	public bool IsValid { get; init; }
	#endregion
}
