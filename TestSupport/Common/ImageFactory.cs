using System;

namespace TestSupport.Common;

/// <summary>
/// Factory methods that build images.
/// </summary>
public static class ImageFactory
{
	#region Methods
	/// <summary>
	/// Creates the PNG of an image of one pixel.
	/// </summary>
	public static byte[] CreatePng()
	{
		return Convert.FromBase64String(
			"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");
	}
	#endregion
}
