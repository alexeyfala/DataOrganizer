using Entities.Abstract;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Entities.Models;

/// <summary>
/// Folder model in a virtual file system.
/// </summary>
public sealed class FolderModel : ExplorerModelBase
{
	#region Properties
	/// <summary>
	/// Child objects.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public Collection<ExplorerModelBase> Children { get; } = [];

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

	/// <summary>
	/// Hash of password for encryption.
	/// </summary>
	public string? PasswordHash { get; init; }
	#endregion
}
