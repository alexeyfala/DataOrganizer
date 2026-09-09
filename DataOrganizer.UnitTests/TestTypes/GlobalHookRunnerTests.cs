using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Messages;
using DataOrganizer.Services;
using Moq;
using Serilog;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

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

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		// Act
		sut.Dispose();

		// Assert
		hook.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// A released key of the owned hook is broadcast as a message.
	/// </summary>
	[Test]
	public async Task KeyReleased_Is_Sent_As_Message()
	{
		// Arrange
		TestGlobalHook hook = new();

		WeakReferenceMessenger messenger = new();

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>(
			TypedParameter.From<IGlobalHook>(hook),
			TypedParameter.From<IMessenger>(messenger));

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
	/// <see cref="GlobalHookRunner.StartAsync" />: runs the hook and does nothing on a second call.
	/// </summary>
	[Test]
	public async Task StartAsync_Runs_Hook_Once()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		// Act
		await sut.StartAsync();

		await sut.StartAsync();

		// Assert
		sut.IsRunning
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StopAsync" />: leaves a hook that is not running untouched.
	/// </summary>
	[Test]
	public async Task StopAsync_Does_Not_Stop_Hook_When_It_Is_Not_Running()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		Mock<IGlobalHook> hook = mock.Mock<IGlobalHook>();

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		Func<Task> act = () => sut.StopAsync();

		// Assert
		await act
			.Should()
			.NotThrowAsync();

		hook.Verify(x => x.Stop(), Times.Never);
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StopAsync" />: logs a failure of the native hook instead of propagating it.
	/// </summary>
	[Test]
	public async Task StopAsync_Logs_Exception_When_Stop_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		Mock<IGlobalHook> hook = mock.Mock<IGlobalHook>();

		hook.SetupGet(x => x.IsRunning)
			.Returns(true);

		hook.Setup(x => x.Stop())
			.Throws(new HookException(UioHookResult.Failure));

		Mock<ILogger> logger = mock.Mock<ILogger>();

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

		// Act
		Func<Task> act = () => sut.StopAsync();

		// Assert
		await act
			.Should()
			.NotThrowAsync();

		logger.Verify(
			x => x.Error(It.IsAny<HookException>(), It.IsAny<string>(), It.IsAny<string>()),
			Times.Once);
	}

	/// <summary>
	/// <see cref="GlobalHookRunner.StopAsync" />: stops a running hook.
	/// </summary>
	[Test]
	public async Task StopAsync_Stops_Running_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

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
