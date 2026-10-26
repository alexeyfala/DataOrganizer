using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Documents;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;
using Threading.Enums;
using Threading.Interfaces;
using Threading.UnitTests.Fakes;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(TypingPause)}"" type")]
internal class TypingPauseTests
{
	#region Methods
	/// <summary>
	/// <see cref="TypingPause.Dispose" />: an action already handed to the UI thread does not run once the pause is
	/// disposed.
	/// </summary>
	[Test]
	public void Dispose_Drops_An_Action_Already_Handed_To_The_UI_Thread()
	{
		// Arrange
		int runs = 0;

		Action? posted = null;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDispatcherAccessor dispatcher = Substitute.For<IDispatcherAccessor>();

			dispatcher.Post(Arg.Do<Action>(x => posted = x), Arg.Any<UiPriority>());

			builder.RegisterInstance<Action>(() => runs++);

			builder.RegisterInstance(dispatcher);

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPause sut = mock.Create<TypingPause>();

		sut.Restart();

		time.Advance(TypingPause.Delay);

		// Act
		sut.Dispose();

		posted!();

		// Assert
		runs
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="TypingPause.Dispose" />: the waiting action does not run once the pause is disposed.
	/// </summary>
	[Test]
	public void Dispose_Drops_The_Waiting_Action()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<Action>(() => runs++);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPause sut = mock.Create<TypingPause>();

		sut.Restart();

		// Act
		sut.Dispose();

		time.Advance(TypingPause.Delay);

		// Assert
		runs
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="TypingPause.Flush" />: nothing runs without a waiting action, also after a pause that has ended.
	/// </summary>
	[Test]
	public void Flush_Runs_Nothing_Without_A_Waiting_Action()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<Action>(() => runs++);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPause sut = mock.Create<TypingPause>();

		sut.Flush();

		sut.Restart();

		time.Advance(TypingPause.Delay);

		// Act
		sut.Flush();

		// Assert
		runs
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="TypingPause.Flush" />: the waiting action runs at once and not again when the pause would have ended.
	/// </summary>
	[Test]
	public void Flush_Runs_The_Waiting_Action_At_Once()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<Action>(() => runs++);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPause sut = mock.Create<TypingPause>();

		sut.Restart();

		// Act
		sut.Flush();

		int runsAtOnce = runs;

		time.Advance(TypingPause.Delay);

		// Assert
		runsAtOnce
			.Should()
			.Be(1);

		runs
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="TypingPause.Restart" />: the action is handed to the UI thread instead of running on the thread of the
	/// clock.
	/// </summary>
	[Test]
	public void Restart_Hands_The_Action_To_The_UI_Thread()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		IDispatcherAccessor dispatcher = Substitute.For<IDispatcherAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<Action>(() => runs++);

			builder.RegisterInstance(dispatcher);

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPause sut = mock.Create<TypingPause>();

		sut.Restart();

		// Act
		time.Advance(TypingPause.Delay);

		// Assert
		runs
			.Should()
			.Be(0);

		dispatcher
			.Received(1)
			.Post(Arg.Any<Action>(), Arg.Any<UiPriority>());
	}

	/// <summary>
	/// <see cref="TypingPause.Restart" />: each change puts the action off by a whole pause again.
	/// </summary>
	[Test]
	public void Restart_Puts_The_Action_Off_While_Typing_Goes_On()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<Action>(() => runs++);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPause sut = mock.Create<TypingPause>();

		sut.Restart();

		time.Advance(TypingPause.Delay / 2.0);

		// Act
		sut.Restart();

		time.Advance(TypingPause.Delay / 2.0);

		int runsWhileTyping = runs;

		time.Advance(TypingPause.Delay / 2.0);

		// Assert
		runsWhileTyping
			.Should()
			.Be(0);

		runs
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="TypingPause.Restart" />: the same pause runs the action again after the next change.
	/// </summary>
	[Test]
	public void Restart_Runs_The_Action_Once_For_Each_Pause()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<Action>(() => runs++);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPause sut = mock.Create<TypingPause>();

		sut.Restart();

		time.Advance(TypingPause.Delay);

		time.Advance(TypingPause.Delay);

		// Act
		sut.Restart();

		time.Advance(TypingPause.Delay);

		// Assert
		runs
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="TypingPause.Restart" />: the action runs once when typing pauses, not before.
	/// </summary>
	[Test]
	public void Restart_Runs_The_Action_Once_Typing_Pauses()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<Action>(() => runs++);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPause sut = mock.Create<TypingPause>();

		// Act
		sut.Restart();

		time.Advance(TypingPause.Delay - TimeSpan.FromMilliseconds(1));

		int runsInPause = runs;

		time.Advance(TimeSpan.FromMilliseconds(1));

		// Assert
		runsInPause
			.Should()
			.Be(0);

		runs
			.Should()
			.Be(1);
	}
	#endregion
}
