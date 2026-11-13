using Avalonia.Input;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Application-scoped clipboard marker of a payload put back from the clipboard log, which tells its echo from a copy made
/// elsewhere.
/// </summary>
internal static class ClipboardRestoreMarker
{
	#region Data
	/// <summary>
	/// Identifier of the marker format.
	/// </summary>
	public const string Identifier = "DataOrganizer.Restore";

	/// <summary>
	/// Marker format.
	/// </summary>
	private static readonly DataFormat<byte[]> Format = DataFormat.CreateBytesApplicationFormat(Identifier);
	#endregion

	#region Methods
	/// <summary>
	/// Attaches the marker to <paramref name="item" />.
	/// </summary>
	public static void Attach(DataTransferItem item) => item.Set(Format, [1]);

	/// <summary>
	/// <c>True</c> when <paramref name="formats" /> carries the marker.
	/// </summary>
	public static bool IsAmong(IReadOnlyList<DataFormat> formats)
	{
		return formats.Any(static format => format.Identifier == Format.Identifier);
	}
	#endregion
}
