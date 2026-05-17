using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Abstract;
using System.Diagnostics;

namespace DataOrganizer.Models;

[DebuggerDisplay($"{nameof(Type)} = {{{nameof(Type)}}}, {nameof(Value)} = {{{nameof(Value)}}}")]
public partial class ValueRecord : DatasetRecordBase
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> when <see cref="Value" /> should be hidden.
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
