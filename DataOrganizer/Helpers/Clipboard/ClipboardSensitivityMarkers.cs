namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Clipboard format identifiers that password managers set to flag content as sensitive
/// (so clipboard history / cloud sync skip it). Used both to detect and to re-emit markers.
/// </summary>
internal static class ClipboardSensitivityMarkers
{
	#region Data
	/// <summary>
	/// Windows / Cloud Clipboard: include in clipboard history (DWORD 0 = exclude).
	/// </summary>
	public const string CanIncludeInClipboardHistory = "CanIncludeInClipboardHistory";

	/// <summary>
	/// Windows / Cloud Clipboard: upload to cloud clipboard (DWORD 0 = exclude).
	/// </summary>
	public const string CanUploadToCloudClipboard = "CanUploadToCloudClipboard";

	/// <summary>
	/// Windows: origin indicator (not a secrecy flag) tagging content re-published from clipboard history (Win+V),
	/// where the accompanying exclude marker is anti-loop rather than a sensitivity signal.
	/// </summary>
	public const string ClipboardHistoryItemId = "ClipboardHistoryItemId";

	/// <summary>
	/// De-facto convention honored by third-party clipboard managers (KeePass et al.).
	/// </summary>
	public const string ClipboardViewerIgnore = "Clipboard Viewer Ignore";

	/// <summary>
	/// Windows: exclude content from clipboard monitor processing (history + cloud).
	/// </summary>
	public const string ExcludeFromMonitorProcessing = "ExcludeClipboardContentFromMonitorProcessing";

	/// <summary>
	/// Linux (KDE Klipper / CopyQ): hint MIME type, value "secret".
	/// </summary>
	public const string KdePasswordManagerHint = "x-kde-passwordManagerHint";

	/// <summary>
	/// macOS: NSPasteboard concealed type convention.
	/// </summary>
	public const string NsPasteboardConcealedType = "org.nspasteboard.ConcealedType";
	#endregion
}
