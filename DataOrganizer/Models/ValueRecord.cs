using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Abstract;
using System.Diagnostics;

namespace DataOrganizer.Models;

[DebuggerDisplay($"{nameof(Type)} = {{{nameof(Type)}}}, {nameof(Value)} = {{{nameof(Value)}}}")]
public partial class ValueRecord : DatasetRecordBase
{
	#region Auto-Generated Properties
	/// <summary>
	/// Returns <c>True</c> when <see cref="Value" /> should be hidden.
	/// </summary>
	[ObservableProperty]
	private bool _isHidden;

	/// <summary>
	/// Value.
	/// </summary>
	[ObservableProperty]
	private string? _value;
	#endregion
}
