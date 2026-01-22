using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Interfaces;
using System.Diagnostics;

namespace DataOrganizer.DTO;

[DebuggerDisplay($"{nameof(Id)} = {{{nameof(Id)}}}, {nameof(Name)} = {{{nameof(Name)}}}")]
[ObservableObject]
public sealed partial class FavoriteCategory : EntityModelBaseDto, IName
{
	#region Properties
	/// <summary>
	/// Child objects.
	/// </summary>
	public required FileModelDto[] Children { get; init; }

	/// <summary>
	/// Name.
	/// </summary>
	public required string Name { get; init; }
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Order.
	/// </summary>
	[ObservableProperty]
	private int _order;
	#endregion
}
