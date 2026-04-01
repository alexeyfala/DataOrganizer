using DataOrganizer.Extensions;
using System;
using System.Runtime.InteropServices;

namespace DataOrganizer.Helpers;

internal sealed class PinnedSecret : IDisposable
{
	#region Properties
	public int Length => _buffer.Length;
	#endregion

	#region Data
	private readonly char[] _buffer;

	private readonly GCHandle _handle;

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
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		MemoryMarshal
			.AsBytes(_buffer.AsSpan())
			.ZeroMemory();

		_handle.Free();
	}
	#endregion
}
