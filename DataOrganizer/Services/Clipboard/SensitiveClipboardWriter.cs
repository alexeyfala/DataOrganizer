using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;

namespace DataOrganizer.Services.Clipboard;

public sealed class SensitiveClipboardWriter : ISensitiveClipboardWriter
{
	#region Data
	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;
	#endregion

	#region Constructors
	public SensitiveClipboardWriter(
		IClipboardAccessor clipboard,
		ITaskExceptionHandler exceptionHandler)
	{
		_clipboard = clipboard;

		_exceptionHandler = exceptionHandler;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Write(string text)
	{
		_exceptionHandler.Watch(_clipboard.SetDataAsync(ClipboardSensitivityMarkerWriter.CreateSensitiveText(text)));
	}
	#endregion
}
