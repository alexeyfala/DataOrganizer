namespace DataOrganizer.Enums;

/// <summary>
/// How imported objects are put into the existing list.
/// </summary>
public enum ImportMode
{
	/// <summary>
	/// The choice was not made, so nothing is imported.
	/// </summary>
	None,

	/// <summary>
	/// The imported objects go after the ones already there.
	/// </summary>
	Append,

	/// <summary>
	/// The existing objects are erased and the imported ones take their place.
	/// </summary>
	Replace
}
