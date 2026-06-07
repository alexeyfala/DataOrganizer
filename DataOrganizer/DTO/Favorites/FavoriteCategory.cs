using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataOrganizer.DTO.Favorites;

[DebuggerDisplay($"{nameof(Id)} = {{{nameof(Id)}}}, {nameof(Name)} = {{{nameof(Name)}}}")]
[ObservableObject]
public sealed partial class FavoriteCategory : EntityModelBaseDto, IName
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
