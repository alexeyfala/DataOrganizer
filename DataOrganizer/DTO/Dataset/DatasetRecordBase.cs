using CommunityToolkit.Mvvm.ComponentModel;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.Json.Serialization;

namespace DataOrganizer.DTO.Dataset;

[JsonDerivedType(typeof(KeyValueRecord), "KeyValue")]
[JsonDerivedType(typeof(RecordsGroup), "Group")]
[JsonDerivedType(typeof(ValueRecord), "Value")]
public abstract partial class DatasetRecordBase : ObservableObject
{
	#region Properties
	/// <summary>
	/// Fires whenever <see cref="PulseHighlight" /> is called.
	/// Bound by the view to play a one-shot animation.
	/// </summary>
	[JsonIgnore]
	public Subject<Unit> HighlightSignal { get; } = new();

	/// <summary>
	/// Note.
	/// </summary>
	[ObservableProperty]
	public partial string? Note { get; set; }

	/// <summary>
	/// Record type.
	/// </summary>
	[JsonIgnore]
	public string Type { get; }
	#endregion

	#region Constructors
	protected DatasetRecordBase() => Type = GetType().Name;
	#endregion

	#region Methods
	/// <summary>
	/// Emits a single highlight pulse on <see cref="HighlightSignal" />.
	/// </summary>
	public void PulseHighlight() => HighlightSignal.OnNext(Unit.Default);
	#endregion
}
