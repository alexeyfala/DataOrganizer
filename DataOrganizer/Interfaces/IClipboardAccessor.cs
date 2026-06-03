using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides means to interact with system clipboard.
/// </summary>
public interface IClipboardAccessor
{
	#region Methods
	/// <summary>
	/// Clears any data from the system clipboard in the UI thread.
	/// </summary>
	Task<bool> ClearAsync();

	/// <summary>
	/// Places a text on the clipboard in the UI thread.
	/// </summary>
	Task<bool> SetTextAsync(string text);
	#endregion
}
