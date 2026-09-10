using Entities.Models;
using SharpHook.Data;
using System;
using System.Diagnostics;

namespace DataOrganizer.Dto.Entities;

/// <inheritdoc cref="HotkeyEntity" />
[DebuggerDisplay(
	$"{nameof(Id)} = {{{nameof(Id)}}}, " +
	$"{nameof(Code)} = {{{nameof(Code)}}}, " +
	$"{nameof(Mask)} = {{{nameof(Mask)}}}")]
public sealed class HotkeyDto : EntityDtoBase
{
	#region Properties
	/// <inheritdoc cref="HotkeyEntity.Code" />
	public required KeyCode Code { get; init; }

	/// <inheritdoc cref="HotkeyEntity.Mask" />
	public required EventMask Mask { get; init; }

	/// <inheritdoc cref="HotkeyEntity.OwnerId" />
	public required Guid OwnerId { get; init; }
	#endregion
}
