using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Opens and places the windows beside the main window: the clipboard history and the notepad.
/// </summary>
public interface ISecondaryWindowLauncher
{
	#region Methods
	/// <summary>
	/// Brings the open notepad to the center of the screen of the main window.
	/// </summary>
	void CenterNotepadWindow();

	/// <summary>
	/// Prompts to unlock the persisted clipboard history when required, then opens the window of the clipboard history
	/// on the screen of the main window.
	/// </summary>
	Task ShowClipboardLogWindowAsync();

	/// <summary>
	/// Activates the open notepad or opens a new one on the screen of the main window.
	/// </summary>
	void ShowNotepadWindow();
	#endregion
}
