using Entities.Models;
using System;

namespace Repository.Enums;

/// <summary>
/// Optional (heavy) properties of <see cref="FileModel" /> that can be opted into
/// when loading entities from the database. Combine with bitwise OR.
/// </summary>
[Flags]
public enum OptionalFileProperty : byte
{
	None = 0,
	Contents = 1 << 0,
	Properties = 1 << 1
}
