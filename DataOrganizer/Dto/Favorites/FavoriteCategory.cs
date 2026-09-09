using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataOrganizer.Dto.Favorites;

[DebuggerDisplay($"{nameof(Id)} = {{{nameof(Id)}}}, {nameof(Name)} = {{{nameof(Name)}}}")]
[ObservableObject]
public sealed partial class FavoriteCategory : EntityModelBaseDto, INamed
{
	#region Properties
	/// <summary>
	/// Child objects.
	/// </summary>
	public required List<FileModelDto> Children { get; init; }

	/// <inheritdoc cref="Enums.EncryptionStatus" />
	public required EncryptionStatus EncryptionStatus { get; init; }

	/// <summary>
	/// Name.
	/// </summary>
	public required string Name { get; init; }
	#endregion
}
