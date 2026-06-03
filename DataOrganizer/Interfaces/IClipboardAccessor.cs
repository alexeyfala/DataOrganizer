using Avalonia.Input.Platform;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides means to interact with system clipboard.
/// </summary>
public interface IClipboardAccessor
{
	#region Methods
	/// <summary>
	/// Searches for a clipboard among windows already running in the application.
	/// </summary>
	IClipboard? FindClipboard();

	/// <inheritdoc cref="ClipboardExtensions.SetTextAsync" />
	Task<bool> SetTextAsync(string text);
	#endregion
}
