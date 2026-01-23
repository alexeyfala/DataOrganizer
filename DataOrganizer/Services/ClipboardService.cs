using Avalonia;
using Avalonia.Input.Platform;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;

namespace DataOrganizer.Services;

public sealed class ClipboardService : IClipboardService
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;
	#endregion

	#region Constructors
	public ClipboardService(Application app) => _app = app;
	#endregion

	#region Methods
	/// <inheritdoc />
	public IClipboard? FindClipboard() => _app.FindClipboard();
	#endregion
}
