using System;
using System.Runtime.InteropServices;

namespace DataOrganizer.Helpers.Security;

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
	/// Wipes a string in memory; an interned instance is left alone.
	/// </summary>
	/// <remarks>
	/// The intern pool is shared by the whole process, so wiping such an instance would corrupt
	/// every literal equal to it. A secret never comes from a literal, so nothing is lost here.
	/// </remarks>
	public static void WipeString(string value)
	{
		if (ReferenceEquals(string.IsInterned(value), value))
		{
			return;
		}

		Span<char> span = MemoryMarshal.CreateSpan(
			ref MemoryMarshal.GetReference(value.AsSpan()),
			value.Length);

		MemoryMarshal
			.AsBytes(span)
			.Clear();
	}
	#endregion
}
