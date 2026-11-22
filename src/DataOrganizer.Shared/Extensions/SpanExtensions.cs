using System;
using System.Security.Cryptography;

namespace DataOrganizer.Shared.Extensions;

/// <summary>
/// Clears byte buffers in a way the compiler cannot optimize away.
/// </summary>
public static class SpanExtensions
{
	#region Methods
	/// <inheritdoc cref="CryptographicOperations.ZeroMemory" />
	public static void ZeroMemory(this Span<byte> buffer) => CryptographicOperations.ZeroMemory(buffer);
	#endregion
}
