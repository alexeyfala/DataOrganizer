using Avalonia.Input;
using DataOrganizer.Helpers.Text;
using System;
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
	/// <c>True</c> when <paramref name="formats" /> carries the ownership marker, i.e. the current clipboard content is a sensitive payload this application placed.
	/// </summary>
	public static bool ContainsOwnershipMarker(IReadOnlyList<DataFormat> formats)
	{
		return formats.Any(static format => format.Identifier == OwnershipFormat.Identifier);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the platform-specific sensitivity markers.
	/// </summary>
	private static (DataFormat<byte[]> Format, byte[] Value)[] BuildMarkersToWrite()
	{
		// Presence is enough for most markers; the Cloud Clipboard ones expect a DWORD 0.
		byte[] presenceMarker = [0];

		byte[] dwordZero = [0, 0, 0, 0];

		if (OperatingSystem.IsWindows())
		{
			return
			[
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing), presenceMarker),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory), dwordZero),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanUploadToCloudClipboard), dwordZero),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ClipboardViewerIgnore), presenceMarker)
			];
		}

		if (OperatingSystem.IsLinux())
		{
			return [(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.KdePasswordManagerHint), TextDefaults.Encoding.GetBytes("secret"))];
		}

		if (OperatingSystem.IsMacOS())
		{
			return [(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.NsPasteboardConcealedType), presenceMarker)];
		}

		return [];
	}
	#endregion
}
