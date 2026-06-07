using SharpHook.Data;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Entities.Models;

/// <summary>
/// Hotkey used when copying content to the clipboard.
/// </summary>
[DebuggerDisplay(
	$"{nameof(Id)} = {{{nameof(Id)}}}, " +
	$"{nameof(Code)} = {{{nameof(Code)}}}, " +
	$"{nameof(Mask)} = {{{nameof(Mask)}}}")]

[XmlType(TypeName = "Hotkey")]
public sealed class HotkeyModel : EntityModelBase
{
	#region Properties
	/// <inheritdoc cref="KeyCode" />
	public required KeyCode Code { get; init; }

	/// <inheritdoc cref="EventMask" />
	public required EventMask Mask { get; init; }

	/// <summary>
	/// Reference to the owner.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public FileModel? Owner { get; init; }

	/// <summary>
	/// Identifier of the owner.
	/// </summary>
	public required Guid OwnerId { get; set; }
	#endregion Properties
}
