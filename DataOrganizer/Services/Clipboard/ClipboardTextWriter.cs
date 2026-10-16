using DataOrganizer.Interfaces.Clipboard;
using Serilog;
using Shared.Extensions;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardTextWriter : IClipboardTextWriter
{
	#region Data
	/// <inheritdoc cref="IClipboardAutoClear" />
	private readonly IClipboardAutoClear _autoClear;

	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public ClipboardTextWriter(
		IClipboardAutoClear autoClear,
		IClipboardAccessor clipboard,
		ILogger logger)
	{
		_autoClear = autoClear;

		_clipboard = clipboard;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> WriteTextAsync(string text, bool isSensitive)
	{
		try
		{
			await _clipboard
				.SetTextAsync(text, isSensitive)
				.ConfigureAwait(false);

			if (isSensitive)
			{
				_autoClear.Arm();
			}

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
	}
	#endregion
}
