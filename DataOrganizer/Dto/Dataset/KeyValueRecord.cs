using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace DataOrganizer.Dto.Dataset;

[DebuggerDisplay(
	$"{nameof(Type)} = {{{nameof(Type)}}}, " +
	$"{nameof(Key)} = {{{nameof(Key)}}}, " +
	$"{nameof(Value)} = {{{nameof(Value)}}}")]
public sealed partial class KeyValueRecord : ValueRecord
{
	#region Properties
	/// <summary>
	/// Key.
	/// </summary>
	[ObservableProperty]
	public partial string? Key { get; set; }
	#endregion
}
