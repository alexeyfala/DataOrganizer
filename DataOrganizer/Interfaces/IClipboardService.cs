using Avalonia.Input.Platform;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides means to interact with system clipboard.
/// </summary>
public interface IClipboardService
{
	#region Methods
	/// <inheritdoc cref="ClipboardExtensions.SetTextAsync" />
	Task<bool> SetTextAsync(string text);
	#endregion
}
