using System.Collections.Generic;
using System.Xml.Serialization;

namespace Entities.Models;

/// <summary>
/// A file in the virtual file system.
/// </summary>
[XmlType(TypeName = FileTypeName)]
public sealed class FileEntity : ExplorerItemBase
{
	#region Properties
	/// <summary>
	/// File contents.
	/// </summary>
	public byte[] Contents { get; init; } = [];

	/// <summary>
	/// Hotkeys used to copy content to the clipboard.
	/// </summary>
	public List<HotkeyEntity> Hotkeys { get; init; } = [];

	/// <summary>
	/// <c>True</c> when the file is marked as a favorite.
	/// </summary>
	public bool IsFavorite { get; init; }

	/// <summary>
	/// Properties in Json format, when using the built-in editor.
	/// </summary>
	public string? Properties { get; init; }
	#endregion
}
