using DataOrganizer.Abstract;

namespace DataOrganizer.DTO;

/// <summary>
/// The pair of <see cref="IsValid" /> and <see cref="StringValueBase.Value" />.
/// </summary>
public sealed class ValueIsValidPair : StringValueBase
{
	#region Properties
	/// <summary>
	/// <c>True</c> when <see cref="StringValueBase.Value" /> is valid.
	/// </summary>
	public bool IsValid { get; init; }
	#endregion
}
