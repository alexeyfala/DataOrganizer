using CommunityToolkit.Mvvm.ComponentModel;

namespace DataOrganizer.Models.Dataset;

/// <summary>
/// Base of the records that hold a value, which can be hidden.
/// </summary>
public abstract partial class ValueRecordBase : DatasetRecordBase
{
	#region Properties
	/// <summary>
	/// <c>True</c> when <see cref="Value" /> should be hidden.
	/// </summary>
	[ObservableProperty]
	public partial bool IsHidden { get; set; }

	/// <summary>
	/// Value.
	/// </summary>
	[ObservableProperty]
	public partial string? Value { get; set; }
	#endregion
}
