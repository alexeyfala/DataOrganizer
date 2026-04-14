using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class ClipboardService : IClipboardService
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;
	#endregion

	#region Constructors
	public ClipboardService(Application app, IDispatcher dispatcher)
	{
		_app = app;

		_dispatcher = dispatcher;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> SetTextAsync(string text)
	{
		if (_app.FindClipboard() is not { } clipboard)
		{
			return false;
		}

		await _dispatcher
			.PostAsync(() => _ = clipboard.SetTextAsync(text))
			.ConfigureAwait(false);

		return true;
	}
	#endregion
}
