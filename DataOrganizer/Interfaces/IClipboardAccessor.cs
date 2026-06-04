using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
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
	/// Gets a list containing the formats currently available from the clipboard in the UI thread.
	/// </summary>
	Task<IReadOnlyList<DataFormat>> GetDataFormatsAsync();

	/// <summary>
	/// Places a data object on the clipboard in the UI thread.
	/// The data object is responsible for providing supported formats and data upon request.
	/// </summary>
	Task SetDataAsync(DataTransfer transfer);

	/// <summary>
	/// Places a text on the clipboard in the UI thread.
	/// </summary>
	Task<bool> SetTextAsync(string text);

	/// <summary>
	/// Returns a bitmap, if available, from the clipboard in the UI thread.
	/// </summary>
	Task<Bitmap?> TryGetBitmapAsync();

	/// <summary>
	/// Returns a list of files, if available, from the clipboard in the UI thread.
	/// </summary>
	Task<IStorageItem[]?> TryGetFilesAsync();

	/// <summary>
	/// Returns a text, if available, from the clipboard in the UI thread.
	/// </summary>
	Task<string?> TryGetTextAsync();

	/// <summary>
	/// Tries to get a value for a given format from the clipboard in the UI thread.
	/// </summary>
	Task<T?> TryGetValueAsync<T>(DataFormat<T> format) where T : class;
	#endregion
}
