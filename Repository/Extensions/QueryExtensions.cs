using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Repository.Extensions;

internal static class QueryExtensions
{
	#region Methods
	/// <summary>
	/// Includes all <see cref="FileModel" /> dependencies from navigation properties in the request.
	/// </summary>
	public static IQueryable<FileModel> IncludeDependencies(this IQueryable<FileModel> source)
	{
		return source.Include(x => x.Hotkeys);
	}
	#endregion Methods
}
