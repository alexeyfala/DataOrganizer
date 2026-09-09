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
	$"{nameof(EntityType)} = {{{nameof(EntityType)}}}, " +
	$"{nameof(Name)} = {{{nameof(Name)}}}")]

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FolderEntity), Folder)]
[JsonDerivedType(typeof(FileEntity), File)]

[XmlType(TypeName = "Entry")]
[XmlInclude(typeof(FolderEntity))]
[XmlInclude(typeof(FileEntity))]
public abstract class ExplorerItemBase : EntityBase
{
	#region Properties
	/// <summary>
	/// Date of creation.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public DateTime CreatedDate { get; set; }

	/// <inheritdoc cref="Enums.EntityType" />
	public EntityKind EntityType { get; init; }

	/// <summary>
	/// <c>True</c> when the object is selected in the list.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public bool IsSelected { get; init; }

	/// <summary>
	/// Name.
	/// </summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// Note.
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
	/// Date of change.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public DateTime UpdatedDate { get; set; }
	#endregion

	#region Data
	/// <summary>
	/// String literal for <see cref="FileEntity" /> derived type.
	/// </summary>
	public const string File = "File";

	/// <summary>
	/// String literal for <see cref="FolderEntity" /> derived type.
	/// </summary>
	public const string Folder = "Folder";
	#endregion
}
