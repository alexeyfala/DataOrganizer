using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Enums.Views;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.Services.Runtime;

[TestFixture(Description = $@"Tests of ""{nameof(UiTimer)}"" type")]
internal class UiTimerTests
{
	#region Data
	/// <summary>
	/// Interval between two ticks.
	/// </summary>
	private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="UiTimer.Run" />: the tick is handed to the UI thread instead of running on the thread of the clock.
	/// </summary>
	[Test]
	public void Run_Hands_The_Tick_To_The_UI_Thread()
	{
		// Arrange
		int ticks = 0;

		FakeTimeProvider time = new();

		IDispatcherAccessor dispatcher = Substitute.For<IDispatcherAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dispatcher);

			builder.RegisterInstance<TimeProvider>(time);
		});

		UiTimer sut = mock.Create<UiTimer>();

		sut.Run(
			() =>
			{
				ticks++;

				return true;
			},
			Interval);

		// Act
		time.Advance(Interval);

		// Assert
		ticks
			.Should()
			.Be(0);

		dispatcher
			.Received(1)
			.Post(Arg.Any<Action>(), Arg.Any<UiPriority>());
	}

	/// <summary>
	/// <see cref="UiTimer.Run" />: the ticks go on while each of them asks for more.
	/// </summary>
	[Test]
	public void Run_Keeps_Ticking_While_The_Tick_Asks_For_More()
	{
		// Arrange
		int ticks = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		UiTimer sut = mock.Create<UiTimer>();

		sut.Run(
			() =>
			{
				ticks++;

				return true;
			},
			Interval);

		// Act
		time.Advance(Interval);

		time.Advance(Interval);

		time.Advance(Interval);

		// Assert
		ticks
			.Should()
			.Be(3);
	}

	/// <summary>
	/// <see cref="UiTimer.Run" />: a tick that answers <c>false</c> is the last one.
	/// </summary>
	[Test]
	public void Run_Stops_Once_The_Tick_Answers_False()
	{
		// Arrange
		int ticks = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		UiTimer sut = mock.Create<UiTimer>();

		sut.Run(
			() =>
			{
				ticks++;

				return false;
			},
			Interval);

		// Act
		time.Advance(Interval);

		time.Advance(Interval);

		time.Advance(Interval);

		// Assert
		ticks
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="UiTimer.Run" />: the first tick comes when the interval has passed.
	/// </summary>
	[Test]
	public void Run_Ticks_When_The_Interval_Has_Passed()
	{
		// Arrange
		int ticks = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		UiTimer sut = mock.Create<UiTimer>();

		sut.Run(
			() =>
			{
				ticks++;

				return true;
			},
			Interval);

		// Act
		time.Advance(Interval);

		// Assert
		ticks
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="UiTimer.Run" />: nothing runs before the interval has passed.
	/// </summary>
	[Test]
	public void Run_Waits_The_Interval_Before_The_First_Tick()
	{
		// Arrange
		int ticks = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		UiTimer sut = mock.Create<UiTimer>();

		sut.Run(
			() =>
			{
				ticks++;

				return true;
			},
			Interval);

		// Act
		time.Advance(Interval - TimeSpan.FromTicks(1));

		// Assert
		ticks
			.Should()
			.Be(0);
	}
	#endregion
}
