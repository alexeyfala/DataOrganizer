using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Abstract;
using System.Collections.ObjectModel;

namespace DataOrganizer.Models;

public sealed partial class RecordsGroup : DatasetRecordBase
{
	#region Properties
	/// <summary>
	/// Child records.
	/// </summary>
	public ObservableCollection<DatasetRecordBase> Children { get; set; } = [];
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Returns <c>True</c> if group is expanded.
	/// </summary>
	[ObservableProperty]
	private bool _isExpanded = true;

	/// <summary>
	/// Name.
	/// </summary>
	[ObservableProperty]
	private string? _name;
	#endregion

	#region Methods
	/// <inheritdoc />
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
