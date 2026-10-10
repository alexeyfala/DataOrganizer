using DataOrganizer.Dto.Settings;

namespace DataOrganizer.Interfaces.Notepad;

/// <summary>
/// Keeps the notepad tabs on the disk: their settings in one file, and their texts in a file for each tab, named by its
/// number.
/// </summary>
public interface INotepadStore
{
	#region Methods
	/// <summary>
	/// Erases the text of a tab from the disk.
	/// </summary>
	void Erase(int number);

	/// <summary>
	/// Returns the numbers of the tabs whose texts are on the disk, in no particular order.
	/// </summary>
	int[] FindNumbers();

	/// <summary>
	/// Returns the bytes of the text of a tab: empty when the tab has no text on the disk, <c>null</c> when the text
	/// cannot be read.
	/// </summary>
	byte[]? Read(int number);

	/// <summary>
	/// Returns the settings of the tabs; <c>null</c> when there are none on the disk or they cannot be read.
	/// </summary>
	NotepadViewSettings? ReadSettings();

	/// <summary>
	/// Puts the bytes of the text of a tab in place of the ones on the disk; <c>false</c> when they cannot be written.
	/// </summary>
	bool Write(int number, byte[] contents);

	/// <summary>
	/// Puts the settings of the tabs in place of the ones on the disk.
	/// </summary>
	void WriteSettings(NotepadViewSettings settings);
	#endregion
}
