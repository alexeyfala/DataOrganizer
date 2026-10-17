using Avalonia.Controls;
using DataOrganizer.Windows;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Opens and closes the windows beside the main window: the clipboard history, the notepad and the console.
/// </summary>
public interface ISecondaryWindowLauncher
{
	#region Methods
	/// <summary>
	/// Brings the open <see cref="NotepadWindow" /> to the center of the screen of <paramref name="owner" />.
	/// </summary>
	void CenterNotepadWindow(Window owner);

	/// <summary>
	/// Closes the console and the notepad, and waits for a console that is still saving its log.
	/// </summary>
	Task CloseSecondaryWindowsAsync();

	/// <summary>
	/// Prompts to unlock the persisted clipboard history when required, then opens
	/// <see cref="ClipboardLogWindow" />.
	/// </summary>
	Task ShowClipboardLogWindowAsync(Window owner);

	/// <summary>
	/// Activates the open <see cref="NotepadWindow" /> or opens a new one.
	/// </summary>
	void ShowNotepadWindow(Window owner);
	#endregion
}
