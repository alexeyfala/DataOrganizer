using DataOrganizer.Models.Clipboard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// The system clipboard in plain data: texts with their formats, images as PNG and files by their paths.
/// </summary>
public interface IClipboardAccessor
{
	#region Methods
	/// <summary>
	/// Empties the system clipboard.
	/// </summary>
	Task ClearAsync();

	/// <summary>
	/// <c>True</c> when the clipboard holds a secret this application placed on it.
	/// </summary>
	Task<bool> ContainsOwnershipMarkerAsync();

	/// <summary>
	/// <c>True</c> when a password manager marked the contents of the clipboard as a secret.
	/// </summary>
	Task<bool> ContainsSensitivityMarkerAsync();

	/// <summary>
	/// Puts files and folders on the clipboard by their paths, leaving out the ones that are gone.
	/// </summary>
	Task SetFilesAsync(IReadOnlyList<ClipboardFileSystemEntry> entries);

	/// <summary>
	/// Puts the image of a PNG on the clipboard.
	/// </summary>
	Task SetImageAsync(byte[] png);

	/// <summary>
	/// Puts a text on the clipboard, marked as a secret when it is sensitive.
	/// </summary>
	Task SetTextAsync(string text, bool isSensitive);

	/// <summary>
	/// Puts a text with its HTML and RTF on the clipboard, marked as a secret when it is sensitive; <c>false</c> when the
	/// clipboard refused it.
	/// </summary>
	Task<bool> SetTextWithFormatsAsync(
		string text,
		string? html,
		string? rtf,
		bool isSensitive);

	/// <summary>
	/// Returns the files and folders on the clipboard, folders first; <c>null</c> without them.
	/// </summary>
	Task<IReadOnlyList<ClipboardFileSystemEntry>?> TryReadFilesAsync();

	/// <summary>
	/// Returns the HTML on the clipboard; <c>null</c> without it.
	/// </summary>
	Task<string?> TryReadHtmlAsync();

	/// <summary>
	/// Returns the image on the clipboard as PNG; <c>null</c> without it.
	/// </summary>
	Task<byte[]?> TryReadImagePngAsync();

	/// <summary>
	/// Returns the RTF on the clipboard; <c>null</c> without it.
	/// </summary>
	Task<string?> TryReadRtfAsync();

	/// <summary>
	/// Returns the text on the clipboard; <c>null</c> without it.
	/// </summary>
	Task<string?> TryReadTextAsync();
	#endregion
}
