using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace DataOrganizer.Models.Dataset;

/// <summary>
/// Record of a dataset that holds a key and a value.
/// </summary>
[DebuggerDisplay(
	$"{nameof(Type)} = {{{nameof(Type)}}}, " +
	$"{nameof(Key)} = {{{nameof(Key)}}}, " +
	$"{nameof(Value)} = {{{nameof(Value)}}}")]
public sealed partial class KeyValueRecord : ValueRecordBase
{
	#region Properties
	/// <summary>
	/// Key.
	/// </summary>
	[ObservableProperty]
	public partial string? Key { get; set; }
	#endregion
}
