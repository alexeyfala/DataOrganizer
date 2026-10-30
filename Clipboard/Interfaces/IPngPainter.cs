using System;

namespace Clipboard.Interfaces;

/// <summary>
/// Draws images pixel by pixel in PNG.
/// </summary>
public interface IPngPainter
{
	#region Methods
	/// <summary>
	/// Returns in PNG an image of a size whose pixels get the ARGB colors of <paramref name="paint" />, which takes the
	/// column and the row of a pixel and the width and the height of the image.
	/// </summary>
	byte[] Paint(
		int width,
		int height,
		Func<int, int, int, int, uint> paint);
	#endregion
}
