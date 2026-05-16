using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Models;
using System.Text.Json.Serialization;

namespace DataOrganizer.Abstract;

[JsonDerivedType(typeof(KeyValueRecord), typeDiscriminator: nameof(KeyValueRecord))]
[JsonDerivedType(typeof(RecordsGroup), typeDiscriminator: nameof(RecordsGroup))]
[JsonDerivedType(typeof(ValueRecord), typeDiscriminator: nameof(ValueRecord))]
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
