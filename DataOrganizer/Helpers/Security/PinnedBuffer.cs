using DataOrganizer.Extensions;
using System;
using System.Threading;

namespace DataOrganizer.Helpers.Security;

/// <summary>
/// A fixed-size byte buffer for key material: never relocated by the GC and wiped on disposal.
/// </summary>
internal sealed class PinnedBuffer : IDisposable
{
	#region Properties
	public int Length => _buffer.Length;
	#endregion

	#region Data
	private readonly byte[] _buffer;

	/// <summary>
	/// <c>True</c> when the buffer has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public PinnedBuffer(int length)
	{
		// Pinned object heap: the array never moves, so a compacting GC cannot leave a copy behind.
		_buffer = GC.AllocateArray<byte>(length, pinned: true);
	}

	public PinnedBuffer(ReadOnlySpan<byte> source) : this(source.Length) => source.CopyTo(_buffer);
	#endregion

	#region Methods
	public ReadOnlySpan<byte> AsReadOnlySpan() => _buffer.AsSpan();

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
