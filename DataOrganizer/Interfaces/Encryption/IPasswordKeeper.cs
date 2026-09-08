using Entities.Models;
using System;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// An object holding the wrapped data encryption key of a protected subtree.
/// </summary>
public interface IPasswordKeeper
{
	#region Properties
	/// <inheritdoc cref="FolderModel.EncryptedDek" />
	byte[]? EncryptedDek { get; set; }

	/// <inheritdoc cref="EntityModelBase.Id" />
	Guid Id { get; }
	#endregion
}
