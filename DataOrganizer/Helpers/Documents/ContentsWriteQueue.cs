using DataOrganizer.Extensions.Text;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataOrganizer.Helpers.Documents;

/// <summary>
/// Writes the contents queued for a file one at a time: only the latest of those waiting, and not when they equal the
/// stored ones. Every queued array is wiped once written or passed over.
/// </summary>
internal sealed class ContentsWriteQueue
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the last contents processed did not reach the file.
	/// </summary>
	public bool HasFailed => Volatile.Read(ref _lastWriteFailed);

	/// <summary>
	/// Clock of the wait for the queued contents.
	/// </summary>
	internal TimeProvider TimeProvider { get; init; } = TimeProvider.System;
	#endregion

	#region Data
	/// <summary>
	/// Time the wait for the queued contents lasts at most.
	/// </summary>
	private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Channel of the contents waiting to be written.
	/// </summary>
	private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
	{
		SingleReader = true,
		SingleWriter = true
	});

	/// <summary>
	/// Guards the number of pending contents together with the signal of their end.
	/// </summary>
	private readonly Lock _pendingGate = new();

	/// <summary>
	/// Writes the contents and answers <c>false</c>, rather than throwing, when they did not reach the file.
	/// </summary>
	private readonly Func<byte[], Task<bool>> _write;

	/// <summary>
	/// <c>True</c> when the last contents processed did not reach the file.
	/// </summary>
	private bool _lastWriteFailed;

	/// <summary>
	/// Number of queued contents that are not processed yet.
	/// </summary>
	private int _pendingCount;

	/// <summary>
	/// Completes once the pending contents are processed; <c>null</c> while nothing is pending.
	/// </summary>
	private TaskCompletionSource? _processed;

	/// <summary>
	/// SHA-256 of the contents the file holds, so that the same contents are not written again.
	/// </summary>
	private byte[]? _storedHash;
	#endregion

	#region Constructors
	public ContentsWriteQueue(Func<byte[], Task<bool>> write) => _write = write;
	#endregion

	#region Methods
	/// <summary>
	/// Ends the queue: the contents queued before are still written, and those queued after are wiped.
	/// </summary>
	public void Complete() => _channel.Writer.TryComplete();

	/// <summary>
	/// Queues contents for writing; the queue wipes them once they are written or passed over.
	/// </summary>
	public void Enqueue(byte[] contents)
	{
		lock (_pendingGate)
		{
			if (_channel
				.Writer
				.TryWrite(contents))
			{
				if (_pendingCount++ == 0)
				{
					_processed = new();
				}

				return;
			}
		}

		contents.ZeroMemory();
	}

	/// <summary>
	/// Writes the queued contents one at a time until the queue is completed.
	/// </summary>
	public async Task RunAsync()
	{
		ChannelReader<byte[]> reader = _channel.Reader;

		await foreach (byte[] contents in reader
			.ReadAllAsync()
			.ConfigureAwait(false))
		{
			// Drain the channel — keep only the latest, ZeroMemory the rest.
			byte[] latest = contents;

			// Counted, not decremented yet: the batch stays pending until it has been persisted.
			int takenCount = 1;

			try
			{
				// Insurance in case of:
				// - slow encryption (large file)
				// - slow DB (disk under load)
				// - quick paste (Ctrl+V of large text can cause several TextChanged in a row)
				while (reader.TryRead(out byte[]? newer))
				{
					takenCount++;

					latest.ZeroMemory();

					latest = newer;
				}

				byte[] hash = SHA256.HashData(latest);

				if (_storedHash is { } previous && hash.AsSpan().SequenceEqual(previous))
				{
					latest.ZeroMemory();

					Volatile.Write(ref _lastWriteFailed, false);

					continue;
				}

				try
				{
					bool isWritten = await _write(latest).ConfigureAwait(false);

					if (isWritten)
					{
						_storedHash = hash;
					}

					Volatile.Write(ref _lastWriteFailed, !isWritten);
				}
				finally
				{
					latest.ZeroMemory();
				}
			}
			finally
			{
				EndPending(takenCount);
			}
		}
	}

	/// <summary>
	/// Takes the contents the file holds, so that the same contents are not written again.
	/// </summary>
	public void SetStored(byte[] contents) => _storedHash = SHA256.HashData(contents);

	/// <summary>
	/// Waits until every queued contents is processed; <c>false</c> when that takes too long.
	/// </summary>
	public async ValueTask<bool> WaitUntilProcessedAsync(CancellationToken token = default)
	{
		Task? processed;

		lock (_pendingGate)
		{
			processed = _processed?.Task;
		}

		if (processed is null)
		{
			return true;
		}

		try
		{
			await processed
				.WaitAsync(ProcessingTimeout, TimeProvider, token)
				.ConfigureAwait(false);

			return true;
		}
		catch (TimeoutException)
		{
			return false;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Counts the taken contents as processed and signals the end once none is pending.
	/// </summary>
	private void EndPending(int takenCount)
	{
		TaskCompletionSource? processed = null;

		lock (_pendingGate)
		{
			_pendingCount -= takenCount;

			if (_pendingCount == 0)
			{
				processed = _processed;

				_processed = null;
			}
		}

		processed?.TrySetResult();
	}
	#endregion
}
