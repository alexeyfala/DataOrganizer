using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using Diagnostics.Interfaces;
using GlobalHotkeys.Messages;
using GlobalHotkeys.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Serilog;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GlobalHotkeys.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(GlobalHookRunner)}"" type")]
internal class GlobalHookRunnerTests
{
	#region Methods
	/// <summary>
	/// <see cref="GlobalHookRunner.Dispose" />: disposes the owned hook.
	/// </summary>
	[Test]
	public void Dispose_Disposes_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IGlobalHook>(hook));

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		sut.Dispose();

		// Assert
		hook.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.Dispose" />: a second disposal disposes the hook no more.
	/// </summary>
	[Test]
	public void Dispose_Disposes_The_Hook_Once()
	{
		// Arrange
		IGlobalHook hook = Substitute.For<IGlobalHook>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(hook));

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		sut.Dispose();

		sut.Dispose();

		// Assert
		hook
			.Received(1)
			.Dispose();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.Dispose" />: a disposed runner refuses to start.
	/// </summary>
	[Test]
	public async Task Dispose_Refuses_A_Later_Start()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(Substitute.For<IGlobalHook>()));

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		sut.Dispose();

		// Assert
		Func<Task> start = () => sut.StartAsync();

		await start
			.Should()
			.ThrowAsync<ObjectDisposedException>();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.Dispose" />: a disposed runner sends no released keys of the hook any more.
	/// </summary>
	[Test]
	public void Dispose_Stops_Sending_The_Released_Keys()
	{
		// Arrange
		IGlobalHook hook = Substitute.For<IGlobalHook>();

		StrongReferenceMessenger messenger = new();

		List<GlobalKeyReleasedMessage> received = [];

		messenger.Register<GlobalKeyReleasedMessage>(
			received,
			static (recipient, message) => ((List<GlobalKeyReleasedMessage>)recipient).Add(message));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(hook);

			builder.RegisterInstance<IMessenger>(messenger);
		});

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		sut.Dispose();

		// Assert
		UioHookEvent released = new()
		{
			Keyboard = new KeyboardEventData
			{
				KeyCode = KeyCode.VcA
			},
			Type = EventType.KeyReleased
		};

		hook.KeyReleased += Raise.EventWith(new KeyboardHookEventArgs(released));

		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StartAsync" />: the wait for a hook that never reports itself as enabled ends at its limit
	/// on the clock.
	/// </summary>
	[Test]
	public async Task StartAsync_Ends_At_Its_Limit_When_The_Hook_Does_Not_Report_Itself_Enabled()
	{
		// Arrange
		// Without a context the end of the wait runs on this thread, so the start shows at once that it is over.
		SynchronizationContext.SetSynchronizationContext(null);

		FakeTimeProvider time = new();

		using ManualResetEventSlim stopped = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IGlobalHook hook = Substitute.For<IGlobalHook>();

			// The hook runs until it is let go and never reports itself as enabled.
			hook
				.When(x => x.Run(GlobalHookType.Keyboard))
				.Do(_ => stopped.Wait());

			builder.RegisterInstance(hook);

			builder.RegisterInstance<TimeProvider>(time);
		});

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		Task starting = sut.StartAsync();

		// Act
		time.Advance(TimeSpan.FromSeconds(1) - TimeSpan.FromTicks(1));

		bool isWaitingBeforeTheLimit = !starting.IsCompleted;

		time.Advance(TimeSpan.FromTicks(1));

		bool isOverAtTheLimit = starting.IsCompleted;

		stopped.Set();

		await starting;

		// Assert
		isWaitingBeforeTheLimit
			.Should()
			.BeTrue();

		isOverAtTheLimit
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StartAsync" />: a hook that fails to run ends the start at once, as it will never report
	/// itself as enabled.
	/// </summary>
	[Test]
	public async Task StartAsync_Ends_At_Once_When_The_Hook_Cannot_Run()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IGlobalHook hook = Substitute.For<IGlobalHook>();

			hook
				.When(x => x.Run(GlobalHookType.Keyboard))
				.Throw(new InvalidOperationException());

			builder.RegisterInstance(hook);

			// The clock never moves, so only the failed run can end the wait.
			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		Task starting = sut.StartAsync();

		// Assert
		await starting.WaitAsync(TimeSpan.FromSeconds(5));
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StartAsync" />: runs the hook and does nothing on a second call.
	/// </summary>
	[Test]
	public async Task StartAsync_Runs_Hook_Once()
	{
		// Arrange
		ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<TestGlobalHook>()
				.As<IGlobalHook>();

			builder.RegisterInstance(exceptionHandler);
		});

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		await sut.StartAsync();

		await sut.StartAsync();

		// Assert
		sut.IsRunning
			.Should()
			.BeTrue();

		// One run of the hook, whose failure the handler sees.
		exceptionHandler
			.Received(1)
			.Watch(Arg.Any<Task>());
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StartAsync" />: a released key of the owned hook is broadcast as a message.
	/// </summary>
	[Test]
	public async Task StartAsync_Sends_A_Released_Key_As_A_Message()
	{
		// Arrange
		TestGlobalHook hook = new();

		WeakReferenceMessenger messenger = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<IGlobalHook>(hook);

			builder.RegisterInstance<IMessenger>(messenger);
		});

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		List<GlobalKeyReleasedMessage> received = [];

		messenger.Register<GlobalKeyReleasedMessage>(
			received,
			static (recipient, message) => ((List<GlobalKeyReleasedMessage>)recipient).Add(message));

		await sut.StartAsync();

		// Act
		hook.SimulateKeyRelease(KeyCode.VcA);

		// Assert
		received
			.Should()
			.ContainSingle();

		received[0].Code
			.Should()
			.Be(KeyCode.VcA);
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StopAsync" />: leaves a hook that is not running untouched.
	/// </summary>
	[Test]
	public async Task StopAsync_Does_Not_Stop_Hook_When_It_Is_Not_Running()
	{
		// Arrange
		IGlobalHook hook = Substitute.For<IGlobalHook>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(hook));

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		Func<Task> act = () => sut.StopAsync();

		// Assert
		await act
			.Should()
			.NotThrowAsync();

		hook
			.DidNotReceive()
			.Stop();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StopAsync" />: the stop ends only once the run of the hook returns, not when the hook
	/// counts itself stopped.
	/// </summary>
	[Test]
	public async Task StopAsync_Ends_Only_After_The_Hook_Run_Returns()
	{
		// Arrange
		using ManualResetEventSlim released = new();

		using ManualResetEventSlim running = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IGlobalHook hook = Substitute.For<IGlobalHook>();

			hook
				.IsRunning
				.Returns(_ => running.IsSet);

			// Like SharpHook, the hook counts itself stopped before its run lets go of the native hook and returns.
			hook
				.When(x => x.Run(GlobalHookType.Keyboard))
				.Do(_ =>
				{
					running.Set();

					hook.HookEnabled += Raise.EventWith(new HookEventArgs(new UioHookEvent()));

					released.Wait();
				});

			hook
				.When(x => x.Stop())
				.Do(_ => running.Reset());

			builder.RegisterInstance(hook);
		});

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		await sut.StartAsync();

		// Act
		Task stopping = sut.StopAsync();

		bool isOverBeforeTheRunReturns = stopping.IsCompleted;

		released.Set();

		await stopping;

		// Assert
		isOverBeforeTheRunReturns
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StopAsync" />: logs a failure of the native hook instead of propagating it.
	/// </summary>
	[Test]
	public async Task StopAsync_Logs_Exception_When_Stop_Fails()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IGlobalHook hook = Substitute.For<IGlobalHook>();

			hook
				.IsRunning
				.Returns(true);

			hook
				.When(x => x.Stop())
				.Throw(new HookException(UioHookResult.Failure));

			builder.RegisterInstance(hook);

			builder.RegisterInstance(logger);
		});

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		Func<Task> act = () => sut.StopAsync();

		// Assert
		await act
			.Should()
			.NotThrowAsync();

		logger
			.Received(1)
			.Error(Arg.Any<HookException>(), Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StopAsync" />: a stop whose wait for an unfinished start is cancelled stops nothing.
	/// </summary>
	[Test]
	public async Task StopAsync_Stops_Nothing_When_The_Wait_For_The_Start_Is_Cancelled()
	{
		// Arrange
		// Without a context the end of the wait runs on this thread, so the start is over when the clock has moved.
		SynchronizationContext.SetSynchronizationContext(null);

		FakeTimeProvider time = new();

		using ManualResetEventSlim stopped = new();

		IGlobalHook hook = Substitute.For<IGlobalHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The hook runs until it is let go and never reports itself as enabled.
			hook
				.When(x => x.Run(GlobalHookType.Keyboard))
				.Do(_ => stopped.Wait());

			// Not running when the start asks, running when the stop would ask.
			hook
				.IsRunning
				.Returns(false, true);

			builder.RegisterInstance(hook);

			builder.RegisterInstance<TimeProvider>(time);
		});

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		Task starting = sut.StartAsync();

		time.Advance(TimeSpan.FromSeconds(1));

		await starting;

		using CancellationTokenSource cancellation = new();

		// Act
		Task stopping = sut.StopAsync(cancellation.Token);

		await cancellation.CancelAsync();

		await stopping;

		stopped.Set();

		// Assert
		hook
			.DidNotReceive()
			.Stop();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StopAsync" />: stops a running hook.
	/// </summary>
	[Test]
	public async Task StopAsync_Stops_Running_Hook()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<TestGlobalHook>()
			.As<IGlobalHook>());

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		await sut.StartAsync();

		sut.IsRunning
			.Should()
			.BeTrue();

		// Act
		await sut.StopAsync();

		// Assert
		sut.IsRunning
			.Should()
			.BeFalse();
	}
	#endregion
}
