using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Hierarchy;
using DataOrganizer.Extensions.Hotkeys;
using Repository.Dto;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Hotkeys;

/// <summary>
/// Finds the file whose hotkey the typed keys end with.
/// </summary>
internal static class HotkeyMatcher
{
	#region Methods
	/// <summary>
	/// Returns the file whose hotkey the typed keys end with, or <c>null</c> if there is none.
	/// </summary>
	public static FileDto? FindFile(
		IEnumerable<ExplorerItemDtoBase> hierarchy,
		KeyStroke[] typed)
	{
		foreach (FileDto file in hierarchy.GetFilesBy(static x => x.Hotkeys.Count > 0))
		{
			KeyStroke[] hotkeys = ReadHotkey(file);

			if (hotkeys.SequenceEqual(typed.TakeLast(hotkeys.Length)))
			{
				return file;
			}
		}

		return null;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the keys of the hotkey of a file with only the modifiers in their masks, as the typed keys have them.
	/// </summary>
	private static KeyStroke[] ReadHotkey(FileDto file) => [.. file
		.Hotkeys
		.ToKeyStrokes()
		.Select(static x => x with
		{
			Mask = x.Mask.ToModifiers()
		})];
	#endregion
}
