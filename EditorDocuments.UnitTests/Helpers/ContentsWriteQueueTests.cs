using AwesomeAssertions;
using EditorDocuments.Helpers;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace EditorDocuments.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(ContentsWriteQueue)}"" type")]
internal class ContentsWriteQueueTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentsWriteQueue.Complete" />: the run ends once the contents queued before are written.
	/// </summary>
	[Test]
	public async Task Complete_Ends_The_Run_After_The_Queued_Contents()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		List<byte[]> written = [];

		ContentsWriteQueue sut = new(x =>
		{
			written.Add([.. x]);

			return Task.FromResult(true);
		});

		sut.Enqueue([.. contents]);

		// Act
		sut.Complete();

		await sut.RunAsync();

		// Assert
		written
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.Enqueue" />: contents queued after the queue has ended are wiped and not written.
	/// </summary>
	[Test]
	public async Task Enqueue_Wipes_The_Contents_After_Complete()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		int writeCount = 0;

		ContentsWriteQueue sut = new(_ =>
		{
			writeCount++;

			return Task.FromResult(true);
		});

		sut.Complete();

		// Act
		sut.Enqueue(contents);

		await sut.RunAsync();

		// Assert
		contents
			.Should()
			.OnlyContain(x => x == 0);

		writeCount
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.HasFailed" />: follows the last write.
	/// </summary>
	[Test]
	public async Task HasFailed_Follows_The_Last_Write([Values] bool isWritten)
	{
		// Arrange
		ContentsWriteQueue sut = new(_ => Task.FromResult(isWritten));

		sut.Enqueue(RandomValues.CreateBytes(10));

		sut.Complete();

		// Act
		await sut.RunAsync();

		// Assert
		sut.HasFailed
			.Should()
			.Be(!isWritten);
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.HasFailed" />: contents equal to the stored ones clear a failure, as the file holds
	/// them.
	/// </summary>
	[Test]
	public async Task HasFailed_Is_Cleared_By_The_Stored_Contents()
	{
		// Arrange
		byte[] stored = RandomValues.CreateBytes(10);

		ContentsWriteQueue sut = new(_ => Task.FromResult(false));

		sut.SetStored(stored);

		Task running = Task.Run(sut.RunAsync);

		sut.Enqueue(RandomValues.CreateBytes(12));

		await sut.WaitUntilProcessedAsync();

		bool hasFailed = sut.HasFailed;

		// Act
		sut.Enqueue([.. stored]);

		sut.Complete();

		await running;

		// Assert
		hasFailed
			.Should()
			.BeTrue();

		sut.HasFailed
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.RunAsync" />: contents equal to the stored ones are wiped and not written.
	/// </summary>
	[Test]
	public async Task RunAsync_Passes_Over_The_Stored_Contents()
	{
		// Arrange
		byte[] stored = RandomValues.CreateBytes(10);

		byte[] contents = [.. stored];

		int writeCount = 0;

		ContentsWriteQueue sut = new(_ =>
		{
			writeCount++;

			return Task.FromResult(true);
		});

		sut.SetStored(stored);

		sut.Enqueue(contents);

		sut.Complete();

		// Act
		await sut.RunAsync();

		// Assert
		writeCount
			.Should()
			.Be(0);

		contents
			.Should()
			.OnlyContain(x => x == 0);
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.RunAsync" />: written contents become the stored ones, so the same contents are not
	/// written again.
	/// </summary>
	[Test]
	public async Task RunAsync_Stores_The_Written_Contents()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		int writeCount = 0;

		ContentsWriteQueue sut = new(_ =>
		{
			writeCount++;

			return Task.FromResult(true);
		});

		Task running = Task.Run(sut.RunAsync);

		sut.Enqueue([.. contents]);

		await sut.WaitUntilProcessedAsync();

		// Act
		sut.Enqueue([.. contents]);

		sut.Complete();

		await running;

		// Assert
		writeCount
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.RunAsync" />: of the contents waiting together, only the latest is written, and the
	/// others are wiped.
	/// </summary>
	[Test]
	public async Task RunAsync_Writes_Only_The_Latest_Of_The_Waiting_Contents()
	{
		// Arrange
		byte[] first = RandomValues.CreateBytes(10);

		byte[] second = RandomValues.CreateBytes(10);

		byte[] latest = RandomValues.CreateBytes(10);

		List<byte[]> written = [];

		ContentsWriteQueue sut = new(x =>
		{
			written.Add([.. x]);

			return Task.FromResult(true);
		});

		sut.Enqueue(first);

		sut.Enqueue(second);

		sut.Enqueue([.. latest]);

		sut.Complete();

		// Act
		await sut.RunAsync();

		// Assert
		written
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Equal(latest);

		first
			.Should()
			.OnlyContain(x => x == 0);

		second
			.Should()
			.OnlyContain(x => x == 0);
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.RunAsync" />: contents whose write failed are written again when queued again.
	/// </summary>
	[Test]
	public async Task RunAsync_Writes_The_Contents_Again_After_A_Failed_Write()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		List<bool> answers = [false, true];

		int writeCount = 0;

		ContentsWriteQueue sut = new(_ => Task.FromResult(answers[writeCount++]));

		Task running = Task.Run(sut.RunAsync);

		sut.Enqueue([.. contents]);

		await sut.WaitUntilProcessedAsync();

		// Act
		sut.Enqueue([.. contents]);

		sut.Complete();

		await running;

		// Assert
		writeCount
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.RunAsync" />: written contents are wiped once the write ends.
	/// </summary>
	[Test]
	public async Task RunAsync_Wipes_The_Written_Contents()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		byte[] expected = [.. contents];

		byte[]? written = null;

		ContentsWriteQueue sut = new(x =>
		{
			written = [.. x];

			return Task.FromResult(true);
		});

		sut.Enqueue(contents);

		sut.Complete();

		// Act
		await sut.RunAsync();

		// Assert
		written
			.Should()
			.Equal(expected);

		contents
			.Should()
			.OnlyContain(x => x == 0);
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.WaitUntilProcessedAsync" />: contents passed over for later ones count as processed,
	/// so the wait answers at once.
	/// </summary>
	[Test]
	public async Task WaitUntilProcessedAsync_Answers_At_Once_After_The_Contents_Waiting_Together()
	{
		// Arrange
		ContentsWriteQueue sut = new(_ => Task.FromResult(true));

		sut.Enqueue(RandomValues.CreateBytes(10));

		sut.Enqueue(RandomValues.CreateBytes(10));

		sut.Enqueue(RandomValues.CreateBytes(10));

		sut.Complete();

		await sut.RunAsync();

		// Act
		ValueTask<bool> waiting = sut.WaitUntilProcessedAsync();

		// Assert
		waiting.IsCompleted
			.Should()
			.BeTrue();

		bool isProcessed = await waiting;

		isProcessed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.WaitUntilProcessedAsync" />: answers <c>false</c> when the queued contents take
	/// too long.
	/// </summary>
	[Test]
	public async Task WaitUntilProcessedAsync_Answers_False_When_The_Write_Takes_Too_Long()
	{
		// Arrange
		FakeTimeProvider time = new();

		TaskCompletionSource<bool> written = new(TaskCreationOptions.RunContinuationsAsynchronously);

		ContentsWriteQueue sut = new(_ => written.Task)
		{
			TimeProvider = time
		};

		sut.Enqueue(RandomValues.CreateBytes(10));

		Task running = sut.RunAsync();

		ValueTask<bool> waiting = sut.WaitUntilProcessedAsync();

		// Act
		time.Advance(TimeSpan.FromSeconds(5));

		bool isProcessed = await waiting;

		written.SetResult(true);

		sut.Complete();

		await running;

		// Assert
		isProcessed
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.WaitUntilProcessedAsync" />: the wait ends as the queued contents are written, with
	/// no pause after them.
	/// </summary>
	[Test]
	public async Task WaitUntilProcessedAsync_Ends_As_The_Queued_Contents_Are_Written()
	{
		// Arrange
		// Without a context the end of the write runs on this thread, so the wait shows at once what follows it.
		SynchronizationContext.SetSynchronizationContext(null);

		TaskCompletionSource<bool> written = new();

		ContentsWriteQueue sut = new(_ => written.Task);

		sut.Enqueue(RandomValues.CreateBytes(10));

		// The run takes the queued contents at once and waits for their write.
		Task running = sut.RunAsync();

		ValueTask<bool> waiting = sut.WaitUntilProcessedAsync();

		// Act
		written.SetResult(true);

		bool isCompleted = waiting.IsCompleted;

		bool isProcessed = await waiting;

		sut.Complete();

		await running;

		// Assert
		isCompleted
			.Should()
			.BeTrue();

		isProcessed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ContentsWriteQueue.WaitUntilProcessedAsync" />: answers once the queued contents are written.
	/// </summary>
	[Test]
	public async Task WaitUntilProcessedAsync_Waits_For_The_Queued_Contents()
	{
		// Arrange
		TaskCompletionSource writing = new(TaskCreationOptions.RunContinuationsAsynchronously);

		TaskCompletionSource<bool> written = new(TaskCreationOptions.RunContinuationsAsynchronously);

		ContentsWriteQueue sut = new(_ =>
		{
			writing.TrySetResult();

			return written.Task;
		});

		Task running = Task.Run(sut.RunAsync);

		sut.Enqueue(RandomValues.CreateBytes(10));

		await writing.Task;

		// Act
		Task<bool> waiting = sut
			.WaitUntilProcessedAsync()
			.AsTask();

		bool wasCompleted = waiting.IsCompleted;

		written.SetResult(true);

		bool isProcessed = await waiting;

		sut.Complete();

		await running;

		// Assert
		wasCompleted
			.Should()
			.BeFalse();

		isProcessed
			.Should()
			.BeTrue();
	}
	#endregion
}
