using DataOrganizer.Extensions;
using System;
using System.Threading;

namespace DataOrganizer.Helpers.Security;

/// <summary>
/// A fixed-size byte buffer for key material: never relocated by the GC and wiped on disposal.
/// </summary>
public sealed class PinnedBuffer : IDisposable
{
	#region Properties
	/// <summary>
	/// Number of bytes the buffer holds.
	/// </summary>
	public int Length => _buffer.Length;
	#endregion

	#region Data
	/// <summary>
	/// Pinned storage of the key material.
	/// </summary>
	private readonly byte[] _buffer;

	/// <summary>
	/// <c>True</c> when the buffer has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	/// <summary>
	/// Creates a zero-filled buffer of the given length.
	/// </summary>
	public PinnedBuffer(int length)
	{
		// Pinned object heap: the array never moves, so a compacting GC cannot leave a copy behind.
		_buffer = GC.AllocateArray<byte>(length, pinned: true);
	}

	/// <summary>
	/// Creates a buffer holding a copy of the source bytes.
	/// </summary>
	public PinnedBuffer(ReadOnlySpan<byte> source) : this(source.Length) => source.CopyTo(_buffer);
	#endregion

	#region Methods
	/// <summary>
	/// Read-only view over the contents.
	/// </summary>
	public ReadOnlySpan<byte> AsReadOnlySpan() => _buffer.AsSpan();

	/// <summary>
	/// Writable view over the contents.
	/// </summary>
	public Span<byte> AsSpan() => _buffer.AsSpan();

	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		_buffer
			.AsSpan()
			.ZeroMemory();
	}
	#endregion
}
