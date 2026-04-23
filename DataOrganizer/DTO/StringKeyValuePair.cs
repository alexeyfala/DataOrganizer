using DataOrganizer.Abstract;

namespace DataOrganizer.DTO;

/// <summary>
/// The pair of string <see cref="Key" /> and <see cref="StringValue.Value" />.
/// </summary>
public sealed class StringKeyValuePair : StringValue
{
	#region Properties
	/// <summary>
	/// Key.
	/// </summary>
	public required string Key { get; init; }
	#endregion
}
