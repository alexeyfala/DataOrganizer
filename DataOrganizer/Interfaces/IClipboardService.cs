using Avalonia.Input.Platform;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides means to interact with system clipboard.
/// </summary>
public interface IClipboardService
{
	#region Methods
	/// <summary>
	/// Tries to get the system clipboard.
	/// </summary>
	IClipboard? FindClipboard();
	#endregion
}
