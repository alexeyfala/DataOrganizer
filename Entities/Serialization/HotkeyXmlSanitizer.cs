using Entities.Models;
using SharpHook.Data;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Entities.Serialization;

/// <summary>
/// Replaces hotkey names that the library no longer knows with the names of the fallback values,
/// so that a single unreadable name does not reject the whole document.
/// </summary>
public static class HotkeyXmlSanitizer
{
	#region Methods
	/// <summary>
	/// Rewrites every hotkey name in <paramref name="document"/> that cannot be read.
	/// </summary>
	public static void Sanitize(XDocument document)
	{
		foreach (XElement hotkey in document.Descendants(HotkeyEntity.HotkeyElementName))
		{
			if (hotkey.Element(nameof(HotkeyEntity.Code)) is { } code
				&& EnumNameReader.Read(code.Value, KeyCode.VcUndefined) == KeyCode.VcUndefined)
			{
				code.Value = nameof(KeyCode.VcUndefined);
			}

			if (hotkey.Element(nameof(HotkeyEntity.Mask)) is { } mask && !IsKnownMask(mask.Value))
			{
				mask.Value = nameof(EventMask.None);
			}
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Tells whether every flag of a mask is a name that the library still has.
	/// </summary>
	private static bool IsKnownMask(string mask)
	{
		string[] names = mask.Split(
			Separators,
			StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		return names.Length != 0 && names.All(x => x == nameof(EventMask.None)
			|| EnumNameReader.Read(x, EventMask.None) != EventMask.None);
	}
	#endregion

	#region Data
	/// <summary>
	/// Characters that separate the flags of a mask, both as the serializer writes them
	/// and as the enum itself does.
	/// </summary>
	private static readonly char[] Separators = [' ', ','];
	#endregion
}
