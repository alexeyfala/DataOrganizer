using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Messages.Hotkeys;
using DataOrganizer.Services.Hotkeys;
using NSubstitute;
using Serilog;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Hotkeys;

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
	/// A released key of the owned hook is broadcast as a message.
	/// </summary>
	[Test]
	public async Task KeyReleased_Is_Sent_As_Message()
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
	/// <see cref="GlobalHookRunner.StartAsync" />: runs the hook and does nothing on a second call.
	/// </summary>
	[Test]
	public async Task StartAsync_Runs_Hook_Once()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<TestGlobalHook>()
			.As<IGlobalHook>());

		GlobalHookRunner sut = mock.Create<GlobalHookRunner>();

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
