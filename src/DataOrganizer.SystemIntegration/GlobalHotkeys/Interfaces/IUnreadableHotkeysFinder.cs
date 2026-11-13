using DataOrganizer.Hierarchy.Dto;
using System.Collections.Generic;

namespace DataOrganizer.SystemIntegration.GlobalHotkeys.Interfaces;

/// <summary>
/// Finds the files whose hotkeys could not be read.
/// </summary>
public interface IUnreadableHotkeysFinder
{
	#region Methods
	/// <summary>
	/// Finds the files of <paramref name="hierarchy" /> whose hotkeys could not be read and logs each of them.
	/// </summary>
	FileDto[] Find(IEnumerable<ExplorerItemDtoBase> hierarchy);
	#endregion
}
