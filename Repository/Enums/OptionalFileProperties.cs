using Entities.Models;
using System;

namespace Repository.Enums;

/// <summary>
/// Optional (heavy) properties of <see cref="FileEntity" /> that can be opted into
/// when loading entities from the database. Combine with bitwise OR.
/// </summary>
[Flags]
public enum OptionalFileProperties
{
	/// <summary>
	/// Neither of the heavy properties is loaded.
	/// </summary>
	None = 0,

	/// <summary>
	/// <see cref="FileEntity.Contents" /> is loaded.
	/// </summary>
	Contents = 1 << 0,

	/// <summary>
	/// <see cref="FileEntity.EditorState" /> is loaded.
	/// </summary>
	EditorState = 1 << 1
}
