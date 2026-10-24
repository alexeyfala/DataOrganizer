using System;
using System.Security.Cryptography;

namespace TextProcessing.Extensions;

public static class SpanExtensions
{
	#region Methods
	/// <inheritdoc cref="CryptographicOperations.ZeroMemory" />
	public static void ZeroMemory(this Span<byte> buffer) => CryptographicOperations.ZeroMemory(buffer);
	#endregion
}
