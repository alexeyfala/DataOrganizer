using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Helpers.Threading;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace DataOrganizer.UnitTests.Helpers.Threading;

[TestFixture(Description = $@"Tests of ""{nameof(TimeProviderScheduler)}"" type")]
internal class TimeProviderSchedulerTests
{
	#region Data
	/// <summary>
	/// Time after which a scheduled action is due.
	/// </summary>
	private static readonly TimeSpan DueTime = TimeSpan.FromSeconds(1.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="TimeProviderScheduler.Now" />: the time of the clock.
	/// </summary>
	[Test]
	public void Now_Is_The_Time_Of_The_Clock()
	{
		// Arrange
		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		TimeProviderScheduler sut = mock.Create<TimeProviderScheduler>();

		// Act
		DateTimeOffset now = sut.Now;

		// Assert
		now
			.Should()
			.Be(time.GetUtcNow());
	}

	/// <summary>
	/// <see cref="TimeProviderScheduler.Schedule{TState}(TState, DateTimeOffset, Func{IScheduler, TState, IDisposable})" />:
	/// the action runs at the moment of the clock it was scheduled for, not before.
	/// </summary>
	[Test]
	public void Schedule_At_A_Moment_Runs_The_Action_At_That_Moment()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		TimeProviderScheduler sut = mock.Create<TimeProviderScheduler>();

		using IDisposable scheduled = sut.Schedule(0, time.GetUtcNow() + DueTime, (_, _) =>
		{
			runs++;

			return Disposable.Empty;
		});

		time.Advance(DueTime - TimeSpan.FromTicks(1));

		int runsBefore = runs;

		// Act
		time.Advance(TimeSpan.FromTicks(1));

		// Assert
		runsBefore
			.Should()
			.Be(0);

		runs
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="TimeProviderScheduler.Schedule{TState}(TState, DateTimeOffset, Func{IScheduler, TState, IDisposable})" />:
	/// an action scheduled for a moment that has passed runs at once.
	/// </summary>
	[Test]
	public void Schedule_At_A_Past_Moment_Runs_The_Action_At_Once()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		TimeProviderScheduler sut = mock.Create<TimeProviderScheduler>();

		// Act
		using IDisposable scheduled = sut.Schedule(0, time.GetUtcNow() - DueTime, (_, _) =>
		{
			runs++;

			return Disposable.Empty;
		});

		// Assert
		runs
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="TimeProviderScheduler.Schedule{TState}(TState, TimeSpan, Func{IScheduler, TState, IDisposable})" />: an
	/// action disposed of after its run has what it returned disposed of too.
	/// </summary>
	[Test]
	public void Schedule_Disposed_After_The_Run_Disposes_What_The_Action_Returned()
	{
		// Arrange
		BooleanDisposable returned = new();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		TimeProviderScheduler sut = mock.Create<TimeProviderScheduler>();

		IDisposable scheduled = sut.Schedule(0, DueTime, (_, _) => returned);

		time.Advance(DueTime);

		// Act
		scheduled.Dispose();

		// Assert
		returned.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="TimeProviderScheduler.Schedule{TState}(TState, TimeSpan, Func{IScheduler, TState, IDisposable})" />: an
	/// action disposed of before it is due never runs.
	/// </summary>
	[Test]
	public void Schedule_Disposed_Before_The_Due_Time_Drops_The_Action()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		TimeProviderScheduler sut = mock.Create<TimeProviderScheduler>();

		IDisposable scheduled = sut.Schedule(0, DueTime, (_, _) =>
		{
			runs++;

			return Disposable.Empty;
		});

		// Act
		scheduled.Dispose();

		time.Advance(DueTime);

		// Assert
		runs
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="TimeProviderScheduler.Schedule{TState}(TState, TimeSpan, Func{IScheduler, TState, IDisposable})" />: the
	/// action runs once the clock has counted its due time, not before, and gets the state and the scheduler.
	/// </summary>
	[Test]
	public void Schedule_Runs_The_Action_After_The_Due_Time()
	{
		// Arrange
		const int state = 7;

		int runs = 0;

		IScheduler? passedScheduler = null;

		int passedState = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		TimeProviderScheduler sut = mock.Create<TimeProviderScheduler>();

		using IDisposable scheduled = sut.Schedule(state, DueTime, (scheduler, value) =>
		{
			runs++;

			passedScheduler = scheduler;

			passedState = value;

			return Disposable.Empty;
		});

		time.Advance(DueTime - TimeSpan.FromTicks(1));

		int runsBefore = runs;

		// Act
		time.Advance(TimeSpan.FromTicks(1));

		// Assert
		runsBefore
			.Should()
			.Be(0);

		runs
			.Should()
			.Be(1);

		passedScheduler
			.Should()
			.BeSameAs(sut);

		passedState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="TimeProviderScheduler.Schedule{TState}(TState, Func{IScheduler, TState, IDisposable})" />: an action
	/// without a due time runs at once.
	/// </summary>
	[Test]
	public void Schedule_Without_A_Due_Time_Runs_The_Action_At_Once()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		TimeProviderScheduler sut = mock.Create<TimeProviderScheduler>();

		// Act
		using IDisposable scheduled = sut.Schedule(0, (_, _) =>
		{
			runs++;

			return Disposable.Empty;
		});

		// Assert
		runs
			.Should()
			.Be(1);
	}
	#endregion
}
