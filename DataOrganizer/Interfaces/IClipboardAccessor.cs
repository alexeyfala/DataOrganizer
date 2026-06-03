using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides means to interact with system clipboard.
/// </summary>
public interface IClipboardAccessor
{
	#region Methods
	/// <summary>
	/// Places a text on the clipboard in the UI thread.
	/// </summary>
	Task<bool> SetTextAsync(string text);
	#endregion
}
