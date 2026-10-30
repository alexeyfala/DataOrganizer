using AwesomeAssertions;
using DatasetRecords.Models;
using Shared.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace DatasetRecords.UnitTests.Models;

[TestFixture(Description = "Guards that the records of a dataset left by an earlier build still open and are written the same")]
internal class DatasetRecordFormatCompatibilityTests
{
	#region Data
	/// <summary>
	/// Records of a dataset as an earlier build wrote them: a collapsed group with a note that holds two keys and values,
	/// one of them hidden and with a note, and a value at the root.
	/// </summary>
	private const string RecordedRecords =
		"""[{"$type":"Group","Children":[{"$type":"KeyValue","Key":"Login","IsHidden":false,"Value":"admin","Note":null},{"$type":"KeyValue","Key":"Password","IsHidden":true,"Value":"secret","Note":"Rotate monthly"}],"IsExpanded":false,"Name":"Accounts","Note":"Work"},{"$type":"Value","IsHidden":false,"Value":"https://example.com","Note":null}]""";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SystemTextJsonSerializer.Deserialize{T}(string)" />: the records that an earlier build wrote are read
	/// whole, each as its own kind.
	/// </summary>
	[Test]
	public void Deserialize_Reads_The_Records_Of_An_Earlier_Build()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		// Act
		DatasetRecordBase[]? result = sut.Deserialize<DatasetRecordBase[]>(RecordedRecords);

		// Assert
		result
			.Should()
			.BeEquivalentTo(
				CreateRecords(),
				options => options
					.PreferringRuntimeMemberTypes()
					.Excluding(x => x.Name == nameof(DatasetRecordBase.HighlightSignal))
					.WithStrictOrdering());
	}

	/// <summary>
	/// <see cref="SystemTextJsonSerializer.SerializeToUtf8Bytes{T}" />: the records are written as an earlier build wrote
	/// them, with the same kinds, names and order of the fields.
	/// </summary>
	[Test]
	public void SerializeToUtf8Bytes_Writes_The_Records_As_An_Earlier_Build_Did()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		// Act
		byte[] result = sut.SerializeToUtf8Bytes(CreateRecords());

		// Assert
		Encoding.UTF8.GetString(result)
			.Should()
			.Be(RecordedRecords);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates the records that <see cref="RecordedRecords" /> holds.
	/// </summary>
	private static ObservableCollection<DatasetRecordBase> CreateRecords()
	{
		return
		[
			new RecordsGroup
			{
				Children =
				[
					new KeyValueRecord
					{
						Key = "Login",
						Value = "admin"
					},
					new KeyValueRecord
					{
						IsHidden = true,
						Key = "Password",
						Note = "Rotate monthly",
						Value = "secret"
					}
				],
				IsExpanded = false,
				Name = "Accounts",
				Note = "Work"
			},
			new ValueRecord
			{
				Value = "https://example.com"
			}
		];
	}
	#endregion
}
