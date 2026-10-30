using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DatasetRecords.Models;

/// <summary>
/// Named group of records of a dataset, which can be collapsed.
/// </summary>
public sealed partial class RecordsGroup : DatasetRecordBase
{
	#region Properties
	/// <summary>
	/// Child records.
	/// </summary>
	public ObservableCollection<DatasetRecordBase> Children { get; set; } = [];

	/// <summary>
	/// <c>True</c> when the group is expanded.
	/// </summary>
	[ObservableProperty]
	public partial bool IsExpanded { get; set; } = true;

	/// <summary>
	/// Name.
	/// </summary>
	[ObservableProperty]
	public partial string? Name { get; set; }
	#endregion

	#region Methods
	/// <inheritdoc />
	[ExcludeFromCodeCoverage(Justification = "A diagnostic text of the object")]
	public override string ToString()
	{
		return
			$"{nameof(Type)} = {Type}, " +
			$"{nameof(Name)} = {Name}, " +
			$"{nameof(IsExpanded)} = {IsExpanded}, " +
			$"{nameof(Children)} = {Children.Count}";
	}
	#endregion
}
