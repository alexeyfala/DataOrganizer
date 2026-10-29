using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TextProcessing.Constants;
using TextProcessing.Extensions;

namespace DataProtection.Helpers;

/// <summary>
/// A fixed-size character buffer for secrets: never relocated by the GC and wiped on disposal.
/// </summary>
public sealed class PinnedSecret : IDisposable
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the buffer holds no characters.
	/// </summary>
	public bool IsEmpty => _buffer.Length == 0;

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
	/// <exception cref="ObjectDisposedException">The secret has been disposed.</exception>
	public ReadOnlySpan<char> AsReadOnlySpan()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		return _buffer.AsSpan();
	}

	/// <summary>
	/// Writable view over the contents.
	/// </summary>
	/// <exception cref="ObjectDisposedException">The secret has been disposed.</exception>
	public Span<char> AsSpan()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		return _buffer.AsSpan();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		// Stryker disable once Block,Statement : a second free of the handle cannot be observed safely
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		MemoryMarshal
			.AsBytes(_buffer.AsSpan())
			.ZeroMemory();

		// Stryker disable once Statement : a leaked handle is invisible to a test
		_handle.Free();
	}

	/// <summary>
	/// Encodes the contents as UTF-8 into a new pinned buffer owned by the caller, normalizing the
	/// characters to NFC first, so one secret spells one key whatever the input method produced.
	/// </summary>
	/// <exception cref="ObjectDisposedException">The secret has been disposed.</exception>
	public PinnedBuffer ToUtf8Buffer()
	{
		ReadOnlySpan<char> characters = AsReadOnlySpan();

		// ASCII holds no decomposable character, so it is normalized already and the common
		// secret never becomes a string.
		// Stryker disable once Block : the longer way gives the same bytes; only the string it makes is invisible
		if (Ascii.IsValid(characters))
		{
			return Encode(characters);
		}

		// The framework normalizes strings only, so the detour is unavoidable; both instances
		// are wiped as soon as the bytes are out.
		string source = new(characters);

		string normalized = source.Normalize(NormalizationForm.FormC);

		try
		{
			return Encode(normalized);
		}
		// Stryker disable once Block,Statement : the wiped strings are not reachable from outside
		finally
		{
			StringWiper.Wipe(normalized);

			StringWiper.Wipe(source);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Encodes the characters as UTF-8 into a new pinned buffer.
	/// </summary>
	private static PinnedBuffer Encode(ReadOnlySpan<char> characters)
	{
		PinnedBuffer buffer = new(TextDefaults
			.Encoding
			.GetByteCount(characters));

		TextDefaults
			.Encoding
			.GetBytes(characters, buffer.AsSpan());

		return buffer;
	}
	#endregion
}
