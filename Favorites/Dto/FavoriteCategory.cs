using CommunityToolkit.Mvvm.ComponentModel;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace Favorites.Dto;

/// <summary>
/// The favorite files of one folder, shown as a category of the favorites window.
/// </summary>
[DebuggerDisplay($"{nameof(Id)} = {{{nameof(Id)}}}, {nameof(Name)} = {{{nameof(Name)}}}")]
[ObservableObject]
public sealed partial class FavoriteCategory : EntityDtoBase, INamed
{
	#region Properties
	/// <summary>
	/// Child objects.
	/// </summary>
	public required List<FileDto> Children { get; init; }

	/// <summary>
	/// Encryption status of the folder the category stands for.
	/// </summary>
	public required EncryptionStatus EncryptionStatus { get; init; }

	/// <summary>
	/// Name.
	/// </summary>
	public required string Name { get; init; }
	#endregion
}
