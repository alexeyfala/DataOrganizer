using Bogus;
using DataOrganizer.Models.Dataset;
using Shared.Extensions;
using System.Collections.Generic;

namespace DataOrganizer.Helpers.Dataset;

/// <summary>
/// Records of a dataset made up for trying the application, with values that repeat from run to run.
/// </summary>
internal sealed class SampleRecords
{
	#region Data
	/// <summary>
	/// Share of the values that are secrets and stay hidden.
	/// </summary>
	private const float HiddenShare = 0.3f;

	/// <summary>
	/// Source of the made-up values.
	/// </summary>
	private readonly Faker _faker = SampleFaker.Create();
	#endregion

	#region Methods
	/// <summary>
	/// Creates the required number of <see cref="RecordsGroup" /> objects.
	/// </summary>
	public IEnumerable<RecordsGroup> CreateGroups(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new RecordsGroup
			{
				Name = _faker.Commerce.Department(),
				Note = _faker.Lorem.Paragraph()
			};
		}
	}

	/// <summary>
	/// Creates the required number of <see cref="KeyValueRecord" /> objects.
	/// </summary>
	public IEnumerable<KeyValueRecord> CreateKeyValueRecords(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new KeyValueRecord
			{
				Key = _faker.Internet.DomainName(),
				Note = _faker.Lorem.Paragraph(),
				Value = _faker.Internet.UserName()
			};
		}
	}

	/// <summary>
	/// Creates a sequence of records of every type, with the groups nested to the required number of levels.
	/// </summary>
	public IEnumerable<DatasetRecordBase> CreateRandomRecords(int eachTypes = 1, int levels = 1)
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
	/// Creates the required number of <see cref="ValueRecord" /> objects, a share of them hidden.
	/// </summary>
	public IEnumerable<ValueRecord> CreateValueRecords(int count)
	{
		for (int i = 0; i < count; i++)
		{
			// A secret stays hidden, as a password does.
			bool isHidden = _faker.Random.Bool(HiddenShare);

			yield return new ValueRecord
			{
				IsHidden = isHidden,
				Note = _faker.Lorem.Paragraph(),
				Value = isHidden ? _faker.Internet.Password() : _faker.Internet.Email()
			};
		}
	}
	#endregion
}
