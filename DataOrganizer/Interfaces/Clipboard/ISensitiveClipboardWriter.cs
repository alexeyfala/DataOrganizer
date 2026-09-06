namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// Places a text on the clipboard with the sensitivity markers, which keep it out of the clipboard
/// history and hand it to the auto-clear.
/// </summary>
public interface ISensitiveClipboardWriter
{
	#region Methods
	/// <summary>
	/// Places <paramref name="text" /> on the clipboard.
	/// </summary>
	void Write(string text);
	#endregion
}
