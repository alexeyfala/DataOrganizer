using System;
using System.Runtime.InteropServices;

namespace DataOrganizer.Helpers;

internal static class SecureStringHelper
{
	#region Methods
	/// <summary>
	/// Copies a string into the pinned array and attempts to zero out the original.
	/// </summary>
	public static PinnedSecret CaptureAndWipe(string source)
	{
		PinnedSecret secret = new(source.Length);

		source
			.AsSpan()
			.CopyTo(secret.AsSpan());

		WipeString(source);

		return secret;
	}
	#endregion

	#region Service
	/// <summary>
	/// Tries to wipe a string in memory.
	/// </summary>
	private static void WipeString(string value)
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
