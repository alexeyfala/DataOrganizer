using DataOrganizer.Extensions;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DataOrganizer.Helpers;

internal sealed class PinnedSecret : IDisposable
{
	#region Properties
	public int Length => _buffer.Length;
	#endregion

	#region Data
	private readonly char[] _buffer;

	private readonly GCHandle _handle;

	/// <summary>
	/// Returns <c>True</c> if the service was disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public PinnedSecret(int length)
	{
		_buffer = new char[length];

		_handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned); // Don't let GC copy
	}
	#endregion

	#region Methods
	public ReadOnlySpan<char> AsReadOnlySpan() => _buffer.AsSpan();

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
