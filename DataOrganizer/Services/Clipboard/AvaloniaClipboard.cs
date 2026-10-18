using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Views;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class AvaloniaClipboard : IAvaloniaClipboard
{
	#region Data
	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="IWindowFinder" />
	private readonly IWindowFinder _windowFinder;
	#endregion

	#region Constructors
	public AvaloniaClipboard(
		IDispatcherAccessor dispatcher,
		IWindowFinder windowFinder)
	{
		_dispatcher = dispatcher;

		_windowFinder = windowFinder;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task ClearAsync()
	{
		if (FindClipboard() is not { } clipboard)
		{
			return Task.CompletedTask;
		}

		return _dispatcher.PostAsync(clipboard.ClearAsync);
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<DataFormat>> GetDataFormatsAsync()
	{
		if (FindClipboard() is not { } clipboard)
		{
			return Task.FromResult<IReadOnlyList<DataFormat>>([]);
		}

		return _dispatcher.PostAsync(clipboard.GetDataFormatsAsync);
	}

	/// <inheritdoc />
	public Task SetBitmapAsync(Bitmap bitmap)
	{
		if (FindClipboard() is not { } clipboard)
		{
			return Task.CompletedTask;
		}

		return _dispatcher.PostAsync(() => clipboard.SetBitmapAsync(bitmap));
	}

	/// <inheritdoc />
	public Task SetDataAsync(DataTransfer transfer)
	{
		if (FindClipboard() is not { } clipboard)
		{
			return Task.CompletedTask;
		}

		return _dispatcher.PostAsync(() => clipboard.SetDataAsync(transfer));
	}

	/// <inheritdoc />
	public Task<Bitmap?> TryGetBitmapAsync()
	{
		if (FindClipboard() is not { } clipboard)
		{
			return Task.FromResult(default(Bitmap));
		}

		return _dispatcher.PostAsync(clipboard.TryGetBitmapAsync);
	}

	/// <inheritdoc />
	public Task<IStorageItem[]?> TryGetFilesAsync()
	{
		if (FindClipboard() is not { } clipboard)
		{
			return Task.FromResult(default(IStorageItem[]));
		}

		return _dispatcher.PostAsync(clipboard.TryGetFilesAsync);
	}

	/// <inheritdoc />
	public Task<string?> TryGetTextAsync()
	{
		if (FindClipboard() is not { } clipboard)
		{
			return Task.FromResult(default(string));
		}

		return _dispatcher.PostAsync(clipboard.TryGetTextAsync);
	}

	/// <inheritdoc />
	public Task<T?> TryGetValueAsync<T>(DataFormat<T> format) where T : class
	{
		if (FindClipboard() is not { } clipboard)
		{
			return Task.FromResult(default(T));
		}

		return _dispatcher.PostAsync(() => clipboard.TryGetValueAsync(format));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the clipboard of the first open window.
	/// </summary>
	private IClipboard? FindClipboard() => _windowFinder.FindWindow<Window>()?.Clipboard;
	#endregion
}
