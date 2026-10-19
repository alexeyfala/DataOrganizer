
namespace DataOrganizer.Interfaces.Notepad;

/// <summary>
/// Keeps the texts of the notepad tabs on the disk: reads them and writes each changed one back once typing pauses.
/// </summary>
public interface INotepadTexts
{
	#region Methods
	/// <summary>
	/// Erases the text of a closed tab from the disk; a text that cannot be read stays as it is.
	/// </summary>
	void Erase(INotepadTab tab);

	/// <summary>
	/// Shows the encoding of a new tab, which has nothing on the disk to read, and writes its text after each pause in
	/// typing.
	/// </summary>
	void Follow(INotepadTab tab);

	/// <summary>
	/// Stops writing the text of a closed tab and drops a write that waits for a pause in typing.
	/// </summary>
	void Forget(INotepadTab tab);

	/// <summary>
	/// Reads the text of a tab from the disk, shows its encoding and writes it back after each pause in typing;
	/// <c>false</c> when the text cannot be read, which leaves the tab read-only, with no encoding to choose.
	/// </summary>
	bool Load(INotepadTab tab);

	/// <summary>
	/// Writes the texts that still wait for a pause in typing.
	/// </summary>
	void WritePending();
	#endregion
}
