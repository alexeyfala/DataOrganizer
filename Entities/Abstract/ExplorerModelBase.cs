using Entities.Enums;
using Entities.Models;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Entities.Abstract;

/// <summary>
/// The base model for virtual file system objects.
/// </summary>
[DebuggerDisplay(
	$"{nameof(Id)} = {{{nameof(Id)}}}, " +
	$"{nameof(EntityType)} = {{{nameof(EntityType)}}}, " +
	$"{nameof(Name)} = {{{nameof(Name)}}}")]

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FolderModel), Folder)]
[JsonDerivedType(typeof(FileModel), File)]

[XmlType(TypeName = "Entry")]
[XmlInclude(typeof(FolderModel))]
[XmlInclude(typeof(FileModel))]
public abstract class ExplorerModelBase : EntityModelBase
{
	#region Properties
	/// <summary>
	/// Date of creation.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public DateTime CreatedDate { get; set; }

	/// <inheritdoc cref="Enums.EntityType" />
	public EntityType EntityType { get; init; }

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
	public string? Note { get; init; }

	/// <summary>
	/// Reference to the parent object.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public FolderModel? Parent { get; set; }

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
	/// String literal for <see cref="FileModel" /> derived type.
	/// </summary>
	public const string File = "File";

	/// <summary>
	/// String literal for <see cref="FolderModel" /> derived type.
	/// </summary>
	public const string Folder = "Folder";
	#endregion
}
