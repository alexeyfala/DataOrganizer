using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Entities.Models;

/// <summary>
/// A folder in the virtual file system.
/// </summary>
[XmlType(TypeName = Folder)]
public sealed class FolderEntity : ExplorerItemBase
{
	#region Properties
	/// <summary>
	/// Child objects.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public Collection<ExplorerItemBase> Children { get; } = [];

	/// <summary>
	/// Encrypted DEK (Data Encryption Key)
	/// </summary>
	public byte[]? EncryptedDek { get; init; }

	/// <summary>
	/// <c>True</c> when the folder is expanded.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public bool IsExpanded { get; init; }
	#endregion
}
