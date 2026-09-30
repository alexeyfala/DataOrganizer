using DataOrganizer.Helpers.Hierarchy;
using Entities.Models;
using Repository.Interfaces.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.Extensions;

internal static class DbAccessExtensions
{
	#region Methods
	/// <summary>
	/// Adds a sample hierarchy after the objects of the root.
	/// </summary>
	public static async Task AddSampleObjectsAsync(this IDbAccess dbAccess)
	{
		int rootIndex = await dbAccess
			.CountOfAsync(x => x.ParentId == null)
			.ConfigureAwait(false);

		ExplorerItemBase[] items = SampleHierarchy.Create(rootIndex, DateTime.Now);

		await dbAccess
			.AddFoldersAsync(items.OfType<FolderEntity>())
			.ConfigureAwait(false);

		await dbAccess
			.AddFilesAsync(items.OfType<FileEntity>())
			.ConfigureAwait(false);
	}
	#endregion
}
