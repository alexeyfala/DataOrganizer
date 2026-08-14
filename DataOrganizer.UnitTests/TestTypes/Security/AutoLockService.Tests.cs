using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Messages;
using DataOrganizer.Services.Encryption;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(AutoLockService)}"" type")]
internal class AutoLockServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="AutoLockService.Arm" />: a repeated arm starts over from the delay currently in the settings.
	/// </summary>
	[Test]
	public void Arm_Restarts_With_The_Current_Delay()
	{
		// Arrange
		AppSettings settings = CreateSettings(1);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, settings, time, new WeakReferenceMessenger()));

		AutoLockService sut = mock.Create<AutoLockService>();

		sut.Arm();

		time.Advance(TimeSpan.FromSeconds(30.0));

		settings.AutoLockMinutes = 5;

		// Act
		sut.Arm();

		// Assert
		sut.Remaining
			.Should()
			.Be(TimeSpan.FromMinutes(5.0));
	}

	/// <summary>
	/// <see cref="AutoLockService.Arm" />: the countdown starts from the delay stored in the settings.
	/// </summary>
	[Test]
	public void Arm_Starts_The_Countdown()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => Register(
			builder,
			CreateSettings(15),
			new FakeTimeProvider(),
			new WeakReferenceMessenger()));

		AutoLockService sut = mock.Create<AutoLockService>();

		// Act
		sut.Arm();

		// Assert
		sut.IsArmed
			.Should()
			.BeTrue();

		sut.Remaining
			.Should()
			.Be(TimeSpan.FromMinutes(15.0));
	}

	/// <summary>
	/// <see cref="AutoLockService.Arm" />: the delay of no auto-lock leaves the countdown stopped.
	/// </summary>
	[Test]
	public void Arm_Without_A_Delay_Does_Not_Start_The_Countdown()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => Register(
			builder,
			CreateSettings(0),
			new FakeTimeProvider(),
			new WeakReferenceMessenger()));

		AutoLockService sut = mock.Create<AutoLockService>();

		// Act
		sut.Arm();

		// Assert
		sut.IsArmed
			.Should()
			.BeFalse();

		sut.Remaining
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AutoLockService.Stop" />: a stopped countdown does not lock the session afterwards.
	/// </summary>
	[Test]
	public void Stop_Cancels_The_Countdown()
	{
		// Arrange
		FakeTimeProvider time = new();

		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, CreateSettings(1), time, messenger));

		AutoLockService sut = mock.Create<AutoLockService>();

		List<SessionAutoLockedMessage> received = Capture(messenger);

		sut.Arm();

		// Act
		sut.Stop();

		time.Advance(TimeSpan.FromMinutes(2.0));

		// Assert
		sut
			.Tick()
			.Should()
			.BeFalse();

		sut.IsArmed
			.Should()
			.BeFalse();

		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="AutoLockService.Tick" />: the time left follows the clock while the delay lasts.
	/// </summary>
	[Test]
	public void Tick_Counts_The_Time_Down()
	{
		// Arrange
		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, CreateSettings(1), time, new WeakReferenceMessenger()));

		AutoLockService sut = mock.Create<AutoLockService>();

		sut.Arm();

		time.Advance(TimeSpan.FromSeconds(20.0));

		// Act
		bool keepTicking = sut.Tick();

		// Assert
		keepTicking
			.Should()
			.BeTrue();

		sut.Remaining
			.Should()
			.Be(TimeSpan.FromSeconds(40.0));
	}

	/// <summary>
	/// <see cref="AutoLockService.Tick" />: the expiry stops the countdown and announces the lock once.
	/// </summary>
	[Test]
	public void Tick_Locks_The_Session_When_The_Delay_Elapses()
	{
		// Arrange
		FakeTimeProvider time = new();

		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, CreateSettings(1), time, messenger));

		AutoLockService sut = mock.Create<AutoLockService>();

		List<SessionAutoLockedMessage> received = Capture(messenger);

		sut.Arm();

		time.Advance(TimeSpan.FromMinutes(1.0));

		// Act
		bool keepTicking = sut.Tick();

		// Assert
		keepTicking
			.Should()
			.BeFalse();

		sut.IsArmed
			.Should()
			.BeFalse();

		sut.Remaining
			.Should()
			.BeNull();

		received
			.Should()
			.ContainSingle();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Collects the lock announcements sent through <paramref name="messenger" />.
	/// </summary>
	private static List<SessionAutoLockedMessage> Capture(IMessenger messenger)
	{
		List<SessionAutoLockedMessage> received = [];

		messenger.Register<SessionAutoLockedMessage>(
			received,
			static (recipient, message) => ((List<SessionAutoLockedMessage>)recipient).Add(message));

		return received;
	}

	/// <summary>
	/// Settings carrying the auto-lock delay of a test.
	/// </summary>
	private static AppSettings CreateSettings(int autoLockMinutes)
	{
		AppSettings settings = TestUtils.CreateRandomSettings();

		settings.AutoLockMinutes = autoLockMinutes;

		return settings;
	}

	/// <summary>
	/// Registers the dependencies the service is built from.
	/// </summary>
	private static void Register(
		ContainerBuilder builder,
		AppSettings settings,
		TimeProvider timeProvider,
		IMessenger messenger)
	{
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		settingsStore
			.Settings
			.Returns(settings);

		builder.RegisterInstance(settingsStore);

		builder
			.RegisterInstance(timeProvider)
			.As<TimeProvider>();

		builder.RegisterInstance(messenger);
	}
	#endregion
}
