using DataOrganizer.Extensions.Text;
using Shared.Extensions;
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
	#endregion

	#region Data
	/// <summary>
	/// Channel of the contents waiting to be written.
	/// </summary>
	private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
	{
		SingleReader = true,
		SingleWriter = true
	});

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
		if (_channel
			.Writer
			.TryWrite(contents))
		{
			Interlocked.Increment(ref _pendingCount);

			return;
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
				Interlocked.Add(ref _pendingCount, -takenCount);
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
	public ValueTask<bool> WaitUntilProcessedAsync(CancellationToken token = default)
	{
		Func<bool> isProcessed = () => Volatile.Read(ref _pendingCount) == 0;

		return isProcessed.WaitAsync(millisecondsDelay: 100, maxRepeats: 50, token);
	}
	#endregion
}
