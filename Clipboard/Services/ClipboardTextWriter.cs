using Clipboard.Interfaces;
using System.Threading.Tasks;

namespace Clipboard.Services;

/// <inheritdoc cref="IClipboardTextWriter" />
public sealed class ClipboardTextWriter : IClipboardTextWriter
{
	#region Data
	/// <inheritdoc cref="IClipboardAutoClear" />
	private readonly IClipboardAutoClear _autoClear;

	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;
	#endregion

	#region Constructors
	public ClipboardTextWriter(IClipboardAccessor clipboard, IClipboardAutoClear autoClear)
	{
		_clipboard = clipboard;

		_autoClear = autoClear;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> WriteTextAsync(string text, bool isSensitive)
	{
		bool isWritten = await _clipboard
			.SetTextAsync(text, html: null, rtf: null, isSensitive, isRestored: false)
			.ConfigureAwait(false);

		if (isWritten && isSensitive)
		{
			_autoClear.Arm();
		}

		return isWritten;
	}
	#endregion
}
