using AwesomeAssertions;
using DataOrganizer.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(AsyncRollbackScope)}"" type")]
internal class AsyncRollbackScopeTests
{
	#region Methods
	/// <summary>
	/// <see cref="AsyncRollbackScope.DisposeAsync" />: a throwing rollback does not stop the remaining ones.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Continues_When_A_Rollback_Throws()
	{
		// Arrange
		List<string> order = [];

		AsyncRollbackScope sut = new();

		sut.OnRollback(() =>
		{
			order.Add("first");

			return Task.CompletedTask;
		});

		sut.OnRollback(() => throw new InvalidOperationException());

		sut.OnRollback(() =>
		{
			order.Add("third");

			return Task.CompletedTask;
		});

		// Act
		await sut.DisposeAsync();

		// Assert
		order
			.Should()
			.Equal("third", "first");
	}

	/// <summary>
	/// <see cref="AsyncRollbackScope.DisposeAsync" />: disposing more than once runs the rollbacks only once.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Is_Idempotent_And_Runs_Rollbacks_Once()
	{
		// Arrange
		int count = 0;

		AsyncRollbackScope sut = new();

		sut.OnRollback(() =>
		{
			count++;

			return Task.CompletedTask;
		});

		// Act
		await sut.DisposeAsync();

		await sut.DisposeAsync();

		// Assert
		count
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="AsyncRollbackScope.DisposeAsync" />: runs the registered rollbacks in LIFO order.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Runs_Rollbacks_In_Lifo_Order()
	{
		// Arrange
		List<string> order = [];

		AsyncRollbackScope sut = new();

		sut.OnRollback(() =>
		{
			order.Add("first");

			return Task.CompletedTask;
		});

		sut.OnRollback(() =>
		{
			order.Add("second");

			return Task.CompletedTask;
		});

		sut.OnRollback(() =>
		{
			order.Add("third");

			return Task.CompletedTask;
		});

		// Act
		await sut.DisposeAsync();

		// Assert
		order
			.Should()
			.Equal("third", "second", "first");
	}

	/// <summary>
	/// <see cref="AsyncRollbackScope.DisposeAsync" />: after <see cref="AsyncRollbackScope.Commit" /> no rollback runs.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Skips_Rollbacks_After_Commit()
	{
		// Arrange
		bool ranback = false;

		AsyncRollbackScope sut = new();

		sut.OnRollback(() =>
		{
			ranback = true;

			return Task.CompletedTask;
		});

		sut.Commit();

		// Act
		await sut.DisposeAsync();

		// Assert
		ranback
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="AsyncRollbackScope.OnRollback(Action)" />: a synchronous rollback action runs on disposal.
	/// </summary>
	[Test]
	public async Task OnRollback_Sync_Action_Is_Executed_On_Dispose()
	{
		// Arrange
		bool ranback = false;

		AsyncRollbackScope sut = new();

		sut.OnRollback(() => ranback = true);

		// Act
		await sut.DisposeAsync();

		// Assert
		ranback
			.Should()
			.BeTrue();
	}
	#endregion
}
