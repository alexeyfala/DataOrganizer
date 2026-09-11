using Entities.Models;
using System;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// An object holding the wrapped data encryption key of a protected subtree.
/// </summary>
public interface IPasswordKeeper
{
	#region Properties
	/// <inheritdoc cref="FolderEntity.EncryptedDek" />
	byte[]? EncryptedDek { get; set; }

	/// <inheritdoc cref="EntityBase.Id" />
	Guid Id { get; }
	#endregion
}
