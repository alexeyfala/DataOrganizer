using DataOrganizer.DTO.Dataset;
using DataOrganizer.Helpers;
using Entities.Enums;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
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
			.Repeat(CreateRandomRecords(levels: levels), 20)
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

	/// <summary>
	/// Creates the required number of random <see cref="RecordsGroup" /> objects.
	/// </summary>
	public static IEnumerable<RecordsGroup> CreateGroups(int count)
	{
		string note = TextHelper
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new RecordsGroup()
			{
				Name = $"Group_{AppUtils.CreateRandomString(10)}",
				Note = note
			};
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="KeyValueRecord" /> objects.
	/// </summary>
	public static IEnumerable<KeyValueRecord> CreateKeyValueRecords(int count)
	{
		string note = TextHelper
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new KeyValueRecord()
			{
				Key = $"Key_{AppUtils.CreateRandomString(10)}",
				Value = $"Value_{AppUtils.CreateRandomString(10)}",
				Note = note
			};
		}
	}

	/// <summary>
	/// Creates a random sequence of <see cref="DatasetRecordBase" />.
	/// </summary>
	public static IEnumerable<DatasetRecordBase> CreateRandomRecords(int eachTypes = 1, int levels = 1)
	{
		if (levels < 1)
		{
			yield break;
		}

		foreach (ValueRecord item in CreateValueRecords(eachTypes))
		{
			yield return item;
		}

		foreach (KeyValueRecord item in CreateKeyValueRecords(eachTypes))
		{
			yield return item;
		}

		foreach (RecordsGroup item in CreateGroups(eachTypes))
		{
			if (levels > 1)
			{
				item
					.Children
					.AddRange(CreateRandomRecords(eachTypes, levels - 1));
			}

			yield return item;
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="ValueRecord" /> objects.
	/// </summary>
	public static IEnumerable<ValueRecord> CreateValueRecords(int count)
	{
		string note = TextHelper
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new ValueRecord()
			{
				Value = $"Value_{AppUtils.CreateRandomString(10)}",
				Note = note
			};
		}
	}
	#endregion

	#region Helpers
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
