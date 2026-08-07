using DataOrganizer.DTO.Entities;
using Entities.Enums;
using System;

namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// The processed note of an explorer object.
/// </summary>
/// <param name="Id">Identifier of the object.</param>
/// <param name="EntityType">Type of the object.</param>
/// <param name="Note">The note in its new form.</param>
public sealed record NoteUpdate(
	Guid Id,
	EntityType EntityType,
	byte[] Note)
{
	#region Methods
	/// <summary>
	/// <c>True</c> when the note belongs to a <see cref="FolderModelDto" />.
	/// </summary>
	public bool IsFolderNote() => EntityType == EntityType.Folder;
	#endregion
}
