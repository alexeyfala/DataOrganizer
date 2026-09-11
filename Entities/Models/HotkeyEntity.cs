using Entities.Serialization;
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
[XmlType(TypeName = HotkeyElementName)]
public sealed class HotkeyEntity : EntityBase
{
	#region Properties
	/// <inheritdoc cref="KeyCode" />
	[JsonConverter(typeof(KeyCodeJsonConverter))]
	public required KeyCode Code { get; init; }

	/// <inheritdoc cref="EventMask" />
	[JsonConverter(typeof(EventMaskJsonConverter))]
	public required EventMask Mask { get; init; }

	/// <summary>
	/// Reference to the owner.
	/// </summary>
	[JsonIgnore]
	[XmlIgnore]
	public FileEntity? Owner { get; init; }

	/// <summary>
	/// Identifier of the owner.
	/// </summary>
	public required Guid OwnerId { get; set; }
	#endregion

	#region Data
	/// <summary>
	/// The name of the XML element that holds a hotkey.
	/// </summary>
	public const string HotkeyElementName = "Hotkey";
	#endregion
}
