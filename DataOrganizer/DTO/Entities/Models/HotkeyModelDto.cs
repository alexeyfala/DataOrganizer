using Entities.Models;
using SharpHook.Data;
using System;
using System.Diagnostics;

namespace DataOrganizer.DTO.Entities.Models;

/// <inheritdoc cref="HotkeyModel" />
[DebuggerDisplay(
	$"{nameof(Id)} = {{{nameof(Id)}}}, " +
	$"{nameof(Code)} = {{{nameof(Code)}}}, " +
	$"{nameof(Mask)} = {{{nameof(Mask)}}}")]
public sealed class HotkeyModelDto : EntityModelBaseDto
{
	#region Properties
	/// <inheritdoc cref="HotkeyModel.Code" />
	public required KeyCode Code { get; init; }

	/// <inheritdoc cref="HotkeyModel.Mask" />
	public required EventMask Mask { get; init; }

	/// <inheritdoc cref="HotkeyModel.OwnerId" />
	public required Guid OwnerId { get; init; }
	#endregion Properties	
}
