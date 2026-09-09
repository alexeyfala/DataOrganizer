using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using Repository.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Overwrites the hotkeys of a file in the database and in the hierarchy.
/// </summary>
public interface IFileHotkeyEditor
{
	#region Methods
	/// <summary>
	/// Overwrites the hotkeys of a file, rejecting a sequence already assigned to another file.
	/// </summary>
	Task<OverwriteHotkeysOutcome> OverwriteAsync(
		FileDto dto,
		KeyStroke[] newHotkeys,
		IEnumerable<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default);
	#endregion
}
