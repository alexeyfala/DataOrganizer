using DataOrganizer.Models.Dataset;
using System.Linq;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Factory methods that build records of a dataset.
/// </summary>
public static class DatasetRecordFactory
{
	#region Methods
	/// <summary>
	/// Creates the required number of <see cref="ValueRecord" /> objects without values.
	/// </summary>
	public static DatasetRecordBase[] CreateValueRecords(int count) => [.. Enumerable.Range(0, count).Select(static _ => new ValueRecord())];
	#endregion
}
