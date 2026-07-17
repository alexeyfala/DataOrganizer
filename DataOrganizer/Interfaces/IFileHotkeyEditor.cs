using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using Repository.DTO;
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
	Task<OverwriteHotkeysResult> OverwriteAsync(
		FileModelDto dto,
		CodeMaskPair[] newHotkeys,
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default);
	#endregion
}
