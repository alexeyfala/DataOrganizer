using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace DataOrganizer.Models.Dataset;

[JsonDerivedType(typeof(KeyValueRecord), "KeyValue")]
[JsonDerivedType(typeof(RecordsGroup), "Group")]
[JsonDerivedType(typeof(ValueRecord), "Value")]
public abstract partial class DatasetRecordBase : ObservableObject
{
	#region Properties
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
}
