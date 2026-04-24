using DataOrganizer.Helpers;
using DataOrganizer.ViewModels;
using Entities.Enums;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataOrganizer.Extensions;

internal static class DbAccessExtensions
{
	#region Methods
	/// <inheritdoc cref="AddRandomObjectsAsync(IDbAccess, int, int, int, int, int, byte[], byte[], Guid?)" />
	public static async Task AddRandomObjectsAsync(
		this IDbAccess dbAccess,
		int folders,
		int files,
		int datasets,
		int levels)
	{
		int total = await dbAccess
			.CountOfAsync(x => x.ParentId == null)
			.ConfigureAwait(false);

		string fileText = TextHelper
			.LoremIpsum
			.Repeat(5, Environment.NewLine + Environment.NewLine);

		string records = JsonSerializer.Serialize(Enumerable
			.Repeat(DatasetEditorViewModel.CreateRandomRecords(levels: levels), 20)
			.SelectMany(x => x), AppUtils.JsonOptions);

		await AddRandomObjectsAsync(
			dbAccess,
			folders: folders,
			files: files,
			levels: levels,
			datasets: datasets,
			startIndex: total,
			fileContents: TextHelper.Utf8Encoding.GetBytes(fileText),
			datasetContents: TextHelper.Utf8Encoding.GetBytes(records)).ConfigureAwait(false);
	}
	#endregion

	#region Service
	/// <summary>
	/// Adds random entities to database.
	/// </summary>
	private static async Task AddRandomObjectsAsync(
		IDbAccess dbAccess,
		int folders,
		int files,
		int levels,
		int datasets,
		int startIndex,
		byte[] fileContents,
		byte[] datasetContents,
		Guid? parentId = null)
	{
		if (levels <= 0)
		{
			return;
		}

		--levels;

		for (int i = 0; i < folders; i++)
		{
			AddEntityParameters parameters = new()
			{
				EntityType = EntityType.Folder,
				Index = startIndex++,
				Name = $"{i + 1}_Folder_{AppUtils.CreateRandomString(6)}",
				ParentId = parentId
			};

			if (await dbAccess
				.AddEntityAsync(parameters)
				.ConfigureAwait(false) is { } folder)
			{
				await AddRandomObjectsAsync(
					dbAccess,
					folders: folders,
					files: files,
					levels: levels,
					datasets: datasets,
					startIndex: 0,
					fileContents: fileContents,
					datasetContents: datasetContents,
					parentId: folder.Id).ConfigureAwait(false);
			}
		}

		for (int i = 0; i < files; i++)
		{
			AddEntityParameters parameters = new()
			{
				EntityType = EntityType.File,
				FileContents = fileContents,
				Index = startIndex++,
				Name = $"{i + 1}_File_{AppUtils.CreateRandomString(6)}.{AppUtils.CreateRandomString(3).ToLower()}",
				ParentId = parentId
			};

			await dbAccess
				.AddEntityAsync(parameters)
				.ConfigureAwait(false);
		}

		for (int i = 0; i < datasets; i++)
		{
			AddEntityParameters parameters = new()
			{
				EntityType = EntityType.DataSet,
				FileContents = datasetContents,
				Index = startIndex++,
				Name = $"{i + 1}_Dataset_{AppUtils.CreateRandomString(6)}",
				ParentId = parentId
			};

			await dbAccess
				.AddEntityAsync(parameters)
				.ConfigureAwait(false);
		}
	}
	#endregion
}
