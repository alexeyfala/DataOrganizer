using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Helpers;

namespace DataOrganizer.Extensions;

internal static class FilterEngineExtensions
{
	#region Methods
	/// <summary>
	/// Refreshes <see cref="FilterEngine{}" /> of <see cref="FavoriteCategory" />.
	/// </summary>
	public static void Refresh(this FilterEngine<FavoriteCategory> filter) => filter.IterateSource((x, i) => x.Order = i);

	/// <summary>
	/// Refreshes <see cref="FilterEngine{}" /> of <see cref="FileModelDto" />.
	/// </summary>
	public static void Refresh(this FilterEngine<FileModelDto> filter) => filter.IterateSource((x, i) => x.Order = i);
	#endregion
}
