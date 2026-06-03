using Avalonia;
using Avalonia.Input.Platform;
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
	public IClipboard? FindClipboard() => _app.FindClipboard();

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
	#endregion
}
