using DataOrganizer.Abstract;

namespace DataOrganizer.DTO;

/// <summary>
/// The pair of string <see cref="Key" /> and <see cref="StringValueBase.Value" />.
/// </summary>
public sealed class StringKeyValuePair : StringValueBase
{
	#region Properties
	/// <summary>
	/// Key.
	/// </summary>
	public required string Key { get; init; }
	#endregion
}
