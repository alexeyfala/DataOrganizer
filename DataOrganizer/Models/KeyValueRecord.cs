using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.Json.Serialization;

namespace DataOrganizer.Models;

[DebuggerDisplay(
	$"{nameof(Type)} = {{{nameof(Type)}}}, " +
	$"{nameof(Key)} = {{{nameof(Key)}}}, " +
	$"{nameof(Value)} = {{{nameof(Value)}}}")]
public sealed partial class KeyValueRecord : ValueRecord
{
	#region Properties
	/// <summary>
	/// Fires whenever <see cref="PulseHighlight" /> is called.
	/// Bound by the view to play a one-shot animation.
	/// </summary>
	[JsonIgnore]
	public Subject<Unit> HighlightSignal { get; } = new();

	/// <summary>
	/// Key.
	/// </summary>
	[ObservableProperty]
	public partial string? Key { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// Emits a single highlight pulse on <see cref="HighlightSignal" />.
	/// </summary>
	public void PulseHighlight() => HighlightSignal.OnNext(Unit.Default);
	#endregion
}
