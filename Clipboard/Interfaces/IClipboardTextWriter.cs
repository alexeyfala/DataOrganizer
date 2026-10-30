using System.Threading.Tasks;

namespace Clipboard.Interfaces;

/// <summary>
/// Places a text on the clipboard; a sensitive one carries the markers, which keep it out of the clipboard
/// history and hand it to the auto-clear.
/// </summary>
public interface IClipboardTextWriter
{
	#region Methods
	/// <summary>
	/// Places <paramref name="text" /> on the clipboard; <c>false</c> when the clipboard refused it.
	/// </summary>
	Task<bool> WriteTextAsync(string text, bool isSensitive);
	#endregion
}
