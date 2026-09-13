namespace Entities.Enums;

/// <summary>
/// Kind of object in the virtual file system.
/// </summary>
public enum EntityKind
{
	/// <summary>
	/// A container for other objects.
	/// </summary>
	Folder,

	/// <summary>
	/// An object whose contents are edited as text.
	/// </summary>
	File,

	/// <summary>
	/// An object whose contents are edited as a list of records.
	/// </summary>
	Dataset
}
