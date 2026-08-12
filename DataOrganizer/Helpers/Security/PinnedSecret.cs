using DataOrganizer.Extensions;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DataOrganizer.Helpers.Security;

/// <summary>
/// A fixed-size character buffer for secrets: never relocated by the GC and wiped on disposal.
/// </summary>
internal sealed class PinnedSecret : IDisposable
{
	#region Properties
	/// <summary>
	/// Number of characters the buffer holds.
	/// </summary>
	public int Length => _buffer.Length;
	#endregion

	#region Data
	/// <summary>
	/// Pinned storage of the secret.
	/// </summary>
	private readonly char[] _buffer;

	/// <summary>
	/// Pin that keeps the buffer at a fixed address.
	/// </summary>
	private readonly GCHandle _handle;

	/// <summary>
	/// <c>True</c> when the buffer has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	/// <summary>
	/// Creates a zero-filled buffer of the given length.
	/// </summary>
	public PinnedSecret(int length)
	{
		_buffer = new char[length];

		_handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned); // Don't let GC copy
	}
	#endregion

	#region Methods
	/// <summary>
	/// Read-only view over the contents.
	/// </summary>
	public ReadOnlySpan<char> AsReadOnlySpan() => _buffer.AsSpan();

	/// <summary>
	/// Writable view over the contents.
	/// </summary>
	public Span<char> AsSpan() => _buffer.AsSpan();

	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		MemoryMarshal
			.AsBytes(_buffer.AsSpan())
			.ZeroMemory();

		_handle.Free();
	}
	#endregion
}
