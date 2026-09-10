using Entities.Enums;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Entities.Models;

/// <summary>
/// The base of the objects that make up the virtual file system.
/// </summary>
[DebuggerDisplay(
	$"{nameof(Id)} = {{{nameof(Id)}}}, " +
	$"{nameof(Kind)} = {{{nameof(Kind)}}}, " +
	$"{nameof(Name)} = {{{nameof(Name)}}}")]

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FolderEntity), FolderTypeName)]
[JsonDerivedType(typeof(FileEntity), FileTypeName)]

[XmlType(TypeName = "Entry")]
[XmlInclude(typeof(FolderEntity))]
[XmlInclude(typeof(FileEntity))]
public abstract class ExplorerItemBase : EntityBase
{
	#region Properties
	/// <summary>
	/// When the object was created.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// <c>True</c> when the object is selected in the list.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public bool IsSelected { get; init; }

	/// <inheritdoc cref="EntityKind" />
	public EntityKind Kind { get; init; }

	/// <summary>
	/// Name.
	/// </summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// Note in its stored binary form.
	/// </summary>
	public byte[]? Note { get; init; }

	/// <summary>
	/// Reference to the parent object.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public FolderEntity? Parent { get; set; }

	/// <summary>
	/// Parent object identifier.
	/// </summary>
	public Guid? ParentId { get; set; }

	/// <summary>
	/// When the object was last changed.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public DateTime UpdatedAt { get; set; }
	#endregion

	#region Data
	/// <summary>
	/// The name that identifies <see cref="FileEntity" /> in serialized documents.
	/// </summary>
	public const string FileTypeName = "File";

	/// <summary>
	/// The name that identifies <see cref="FolderEntity" /> in serialized documents.
	/// </summary>
	public const string FolderTypeName = "Folder";
	#endregion
}
