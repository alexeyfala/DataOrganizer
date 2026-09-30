using DataOrganizer.Helpers.Hierarchy;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Models.Dataset;
using Entities.Models;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
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

	/// <summary>
	/// Creates the required number of random <see cref="RecordsGroup" /> objects.
	/// </summary>
	public static IEnumerable<RecordsGroup> CreateGroups(int count)
	{
		string note = SampleText
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new RecordsGroup()
			{
				Name = $"Group_{RandomString.Create(10)}",
				Note = note
			};
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="KeyValueRecord" /> objects.
	/// </summary>
	public static IEnumerable<KeyValueRecord> CreateKeyValueRecords(int count)
	{
		string note = SampleText
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new KeyValueRecord()
			{
				Key = $"Key_{RandomString.Create(10)}",
				Value = $"Value_{RandomString.Create(10)}",
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
		string note = SampleText
			.LoremIpsum
			.Repeat(1, Environment.NewLine + Environment.NewLine);

		for (int i = 0; i < count; i++)
		{
			yield return new ValueRecord()
			{
				Value = $"Value_{RandomString.Create(10)}",
				Note = note
			};
		}
	}
	#endregion
}
