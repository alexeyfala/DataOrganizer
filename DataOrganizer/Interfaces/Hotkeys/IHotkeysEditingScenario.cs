using DataOrganizer.Dto.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Hotkeys;

/// <summary>
/// Lets the user edit the hotkeys of a file while the tracking of the hotkeys pauses.
/// </summary>
public interface IHotkeysEditingScenario
{
	#region Methods
	/// <summary>
	/// Shows the hotkey editor with the hotkeys of the file and writes the hotkeys it saves; the tracking of the hotkeys
	/// pauses meanwhile.
	/// </summary>
	Task EditAsync(FileDto file, IEnumerable<ExplorerItemDtoBase> hierarchy);
	#endregion
}
