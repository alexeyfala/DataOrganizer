using DialogContracts.Dto;
using Repository.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DialogContracts.Interfaces;

/// <summary>
/// Asks the user for the hotkeys of a file.
/// </summary>
public interface IHotkeysPrompt
{
	#region Methods
	/// <summary>
	/// Displays the hotkey editor.
	/// </summary>
	Task<EditingHotkeysResult> EditHotkeysAsync(IEnumerable<KeyStroke> initialHotkeys);
	#endregion
}
