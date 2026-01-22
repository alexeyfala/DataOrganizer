using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace DataOrganizer.Models;

[DebuggerDisplay(
	$"{nameof(Type)} = {{{nameof(Type)}}}, " +
	$"{nameof(Key)} = {{{nameof(Key)}}}, " +
	$"{nameof(Value)} = {{{nameof(Value)}}}")]
public sealed partial class KeyValueRecord : ValueRecord
{
	#region Auto-Generated Properties
	/// <summary>
	/// Used for color animation.
	/// </summary>
	[ObservableProperty]
	[property: System.Text.Json.Serialization.JsonIgnore]
	private bool _isHighlight;

	/// <summary>
	/// Key.
	/// </summary>
	[ObservableProperty]
	private string? _key;
	#endregion
}
