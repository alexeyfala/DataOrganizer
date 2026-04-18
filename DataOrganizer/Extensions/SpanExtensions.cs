using System;
using System.Security.Cryptography;

namespace DataOrganizer.Extensions;

internal static class SpanExtensions
{
	#region Methods
	/// <inheritdoc cref="CryptographicOperations.ZeroMemory" />
	public static void ZeroMemory(this Span<byte> buffer) => CryptographicOperations.ZeroMemory(buffer);
	#endregion
}
