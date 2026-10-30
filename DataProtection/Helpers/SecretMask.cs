using System.Linq;
using TextProcessing.Constants;

namespace DataProtection.Helpers;

/// <summary>
/// Hides a value on screen without telling its length.
/// </summary>
public static class SecretMask
{
	#region Data
	/// <summary>
	/// Text shown in place of every hidden value, whatever its length.
	/// </summary>
	private static readonly string Mask = string.Concat(Enumerable.Repeat(Glyphs.DarkShade, 8));
	#endregion

	#region Methods
	/// <summary>
	/// Returns the text shown for a value: the mask while the value is hidden and not empty, the value itself otherwise.
	/// </summary>
	public static string? Apply(string? value, bool isHidden)
	{
		if (!isHidden || string.IsNullOrEmpty(value))
		{
			return value;
		}

		return Mask;
	}
	#endregion
}
