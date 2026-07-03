using Avalonia.Input;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Clipboard;
using Shared.Common;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Builds and attaches the platform-specific clipboard sensitivity markers so clipboard history.
/// </summary>
internal static class ClipboardSensitivityMarkerWriter
{
	#region Data
	/// <summary>
	/// Sensitivity marker formats written alongside a sensitive payload; platform-specific, empty on unsupported platforms.
	/// </summary>
	private static readonly (DataFormat<byte[]> Format, byte[] Value)[] MarkersToWrite = BuildMarkersToWrite();

	/// <summary>
	/// Application-scoped ownership marker format identifying a sensitive payload this application placed.
	/// </summary>
	private static readonly DataFormat<byte[]> OwnershipFormat = DataFormat.CreateBytesApplicationFormat(ClipboardSensitivityMarkers.AutoClearOwnership);

	/// <summary>
	/// Auto-clear scheduler armed after each sensitive copy; wired once at startup, <c>null</c> until then.
	/// </summary>
	private static IClipboardAutoClear? _autoClear;
	#endregion

	#region Methods
	/// <summary>
	/// Attaches the sensitivity markers to <paramref name="item" />.
	/// </summary>
	public static void AttachSensitivityMarkers(DataTransferItem item)
	{
		foreach ((DataFormat<byte[]> format, byte[] value) in MarkersToWrite)
		{
			item.Set(format, value);
		}

		item.Set(OwnershipFormat, [1]);
	}

	/// <summary>
	/// Wires (or clears) the auto-clear scheduler armed after each sensitive copy.
	/// </summary>
	public static void Configure(IClipboardAutoClear? autoClear) => _autoClear = autoClear;

	/// <summary>
	/// <c>True</c> when <paramref name="formats" /> carries the ownership marker, i.e. the current clipboard content is a sensitive payload this application placed.
	/// </summary>
	public static bool ContainsOwnershipMarker(IReadOnlyList<DataFormat> formats)
	{
		return formats.Any(static format => format.Identifier == OwnershipFormat.Identifier);
	}

	/// <summary>
	/// Builds a single-item <see cref="DataTransfer" /> holding <paramref name="text" /> plus the sensitivity markers.
	/// </summary>
	public static DataTransfer CreateSensitiveText(string text)
	{
		DataTransferItem item = new();

		item.SetText(text);

		AttachSensitivityMarkers(item);

		// Do NOT dispose: ownership of the DataTransfer passes to the clipboard (delayed rendering).
		DataTransfer transfer = new();

		transfer.Add(item);

		// Start the auto-clear countdown for this sensitive copy (no-op until wired at startup).
		_autoClear?.Arm();

		return transfer;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the platform-specific sensitivity markers.
	/// </summary>
	private static (DataFormat<byte[]> Format, byte[] Value)[] BuildMarkersToWrite()
	{
		// Presence is enough for most markers; the Cloud Clipboard ones expect a DWORD 0.
		byte[] present = [0];

		byte[] dwordZero = [0, 0, 0, 0];

		if (AppUtils.IsWindows)
		{
			return
			[
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing), present),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory), dwordZero),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanUploadToCloudClipboard), dwordZero),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ClipboardViewerIgnore), present)
			];
		}

		if (AppUtils.IsLinux)
		{
			return [(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.KdePasswordManagerHint), TextHelper.Utf8Encoding.GetBytes("secret"))];
		}

		if (AppUtils.IsMacOs)
		{
			return [(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.NsPasteboardConcealedType), present)];
		}

		return [];
	}
	#endregion
}
