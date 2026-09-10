using DataOrganizer.Dto.Entities;
using Entities.Enums;
using System;

namespace DataOrganizer.Dto.Encryption;

/// <summary>
/// The processed note of an explorer object.
/// </summary>
/// <param name="Id">Identifier of the object.</param>
/// <param name="Kind">Kind of the object.</param>
/// <param name="Note">The note in its new form.</param>
public sealed record NoteUpdate(
	Guid Id,
	EntityKind Kind,
	byte[] Note)
{
	#region Methods
	/// <summary>
	/// <c>True</c> when the note belongs to a <see cref="FolderDto" />.
	/// </summary>
	public bool IsFolderNote() => Kind == EntityKind.Folder;
	#endregion
}
