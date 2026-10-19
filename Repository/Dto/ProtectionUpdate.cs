using Entities.Models;
using System;
using System.Collections.Generic;

namespace Repository.Dto;

/// <summary>
/// The rewritten contents, notes and wrapped key of a folder whose protection changes.
/// </summary>
public sealed class ProtectionUpdate
{
	#region Properties
	/// <summary>
	/// The new contents of the files, by file identifier.
	/// </summary>
	public required IReadOnlyDictionary<Guid, byte[]> Contents { get; init; }

	/// <inheritdoc cref="FolderEntity.EncryptedDek" />
	public required byte[]? EncryptedDek { get; init; }

	/// <summary>
	/// The new notes of the files, by file identifier; a note goes in only together with the new contents of its file.
	/// </summary>
	public required IReadOnlyDictionary<Guid, byte[]> FileNotes { get; init; }

	/// <summary>
	/// Identifier of the folder whose protection changes.
	/// </summary>
	public required Guid FolderId { get; init; }

	/// <summary>
	/// The new notes of the folders, by folder identifier.
	/// </summary>
	public required IReadOnlyDictionary<Guid, byte[]> FolderNotes { get; init; }

	/// <summary>
	/// The date of change of the files and of the folders whose notes change.
	/// </summary>
	public required DateTime UpdatedAt { get; init; }
	#endregion
}
