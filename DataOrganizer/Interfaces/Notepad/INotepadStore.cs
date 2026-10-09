namespace DataOrganizer.Interfaces.Notepad;

/// <summary>
/// Keeps the texts of the notepad tabs on the disk, a file for each tab, named by its number.
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
	/// Puts the bytes of the text of a tab in place of the ones on the disk; <c>false</c> when they cannot be written.
	/// </summary>
	bool Write(int number, byte[] contents);
	#endregion
}
