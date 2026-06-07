using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace DataOrganizer.DTO.Dataset;

[DebuggerDisplay($"{nameof(Type)} = {{{nameof(Type)}}}, {nameof(Value)} = {{{nameof(Value)}}}")]
public partial class ValueRecord : DatasetRecordBase
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
