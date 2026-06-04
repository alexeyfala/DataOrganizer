using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class ClipboardAccessor : IClipboardAccessor
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;
	#endregion

	#region Constructors
	public ClipboardAccessor(
		Application app,
		IDispatcherAccessor dispatcher,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

		_dispatcher = dispatcher;

		_exceptionHandler = exceptionHandler;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> ClearAsync()
	{
		if (_app.FindClipboard() is not { } clipboard)
		{
			return false;
		}

		await _dispatcher
			.PostAsync(clipboard.ClearAsync)
			.ConfigureAwait(false);

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> SetTextAsync(string text)
	{
		if (_app.FindClipboard() is not { } clipboard)
		{
			return false;
		}

		await _dispatcher
			.PostAsync(() => _exceptionHandler.Watch(clipboard.SetTextAsync(text)))
			.ConfigureAwait(false);

		return true;
	}

	/// <inheritdoc />
	public Task<Bitmap?> TryGetBitmapAsync()
	{
		if (_app.FindClipboard() is not { } clipboard)
		{
			return Task.FromResult(default(Bitmap));
		}

		return _dispatcher.PostAsync(clipboard.TryGetBitmapAsync);
	}

	/// <inheritdoc />
	public Task<IStorageItem[]?> TryGetFilesAsync()
	{
		if (_app.FindClipboard() is not { } clipboard)
		{
			return Task.FromResult(default(IStorageItem[]));
		}

		return _dispatcher.PostAsync(clipboard.TryGetFilesAsync);
	}

	/// <inheritdoc />
	public Task<string?> TryGetTextAsync()
	{
		if (_app.FindClipboard() is not { } clipboard)
		{
			return Task.FromResult(default(string));
		}

		return _dispatcher.PostAsync(clipboard.TryGetTextAsync);
	}
	#endregion
}
