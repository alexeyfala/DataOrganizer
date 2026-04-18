using System;
using System.Runtime.InteropServices;

namespace DataOrganizer.Helpers;

internal static class SecureStringHelper
{
	#region Methods
	/// <summary>
	/// Copies a string into the pinned array and attempts to zero out the original.
	/// </summary>
	public static PinnedSecret CaptureAndWipe(string value)
	{
		PinnedSecret secret = new(value.Length);

		value
			.AsSpan()
			.CopyTo(secret.AsSpan());

		WipeString(value);

		return secret;
	}

	/// <summary>
	/// Wipes a string in memory.
	/// </summary>
	public static void WipeString(string value)
	{
		Span<char> span = MemoryMarshal.CreateSpan(
			ref MemoryMarshal.GetReference(value.AsSpan()),
			value.Length);

		MemoryMarshal
			.AsBytes(span)
			.Clear();
	}
	#endregion
}
