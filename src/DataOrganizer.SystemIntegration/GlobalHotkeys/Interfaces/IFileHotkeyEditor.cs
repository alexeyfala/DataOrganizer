using DataOrganizer.Database.Repository.Dto;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.SystemIntegration.GlobalHotkeys.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.GlobalHotkeys.Interfaces;

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
