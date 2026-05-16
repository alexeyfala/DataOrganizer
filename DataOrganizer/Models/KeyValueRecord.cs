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
	#region Auto-Generated Properties
	/// <summary>
	/// Key.
	/// </summary>
	[ObservableProperty]
	private string? _key;
	#endregion

	#region Properties
	/// <summary>
	/// Fires whenever <see cref="PulseHighlight" /> is called. Bound by the view
	/// to play a one-shot animation.
	/// </summary>
	[JsonIgnore]
	public Subject<Unit> HighlightSignal { get; } = new();
	#endregion

	#region Methods
	/// <summary>
	/// Emits a single highlight pulse on <see cref="HighlightSignal" />.
	/// </summary>
	public void PulseHighlight() => HighlightSignal.OnNext(Unit.Default);
	#endregion
}
