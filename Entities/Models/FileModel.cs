using Entities.Abstract;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Entities.Models;

/// <summary>
/// File model in a virtual file system.
/// </summary>
[XmlType(TypeName = File)]
public sealed class FileModel : ExplorerModelBase
{
	#region Properties
	/// <summary>
	/// File contents.
	/// </summary>
	public byte[] Contents { get; init; } = [];

	/// <summary>
	/// Hotkeys used to copy content to the clipboard.
	/// </summary>
	public List<HotkeyModel> Hotkeys { get; init; } = [];

	/// <summary>
	/// Used in "Favorites" mode.
	/// </summary>
	public bool IsFavorite { get; init; }

	/// <summary>
	/// Properties in Json format, when using the built-in editor.
	/// </summary>
	public string? Properties { get; init; }
	#endregion
}
