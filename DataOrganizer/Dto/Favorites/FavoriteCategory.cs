using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataOrganizer.Dto.Favorites;

[DebuggerDisplay($"{nameof(Id)} = {{{nameof(Id)}}}, {nameof(Name)} = {{{nameof(Name)}}}")]
[ObservableObject]
public sealed partial class FavoriteCategory : EntityDtoBase, INamed
{
	#region Properties
	/// <summary>
	/// Child objects.
	/// </summary>
	public required List<FileDto> Children { get; init; }

	/// <inheritdoc cref="Enums.Encryption.EncryptionStatus" />
	public required EncryptionStatus EncryptionStatus { get; init; }

	/// <summary>
	/// Name.
	/// </summary>
	public required string Name { get; init; }
	#endregion
}
