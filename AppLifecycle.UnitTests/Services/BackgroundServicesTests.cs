using AppLifecycle.Services;
using Autofac;
using Autofac.Extras.Moq;
using Clipboard.Interfaces;
using GlobalHotkeys.Interfaces;
using NSubstitute;
using Settings.Dto;
using Settings.Interfaces;
using Settings.UnitTests.Factories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppLifecycle.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(BackgroundServices)}"" type")]
internal class BackgroundServicesTests
{
	#region Methods
	/// <summary>
	/// <see cref="BackgroundServices.ApplySettingsAsync" />: a clipboard history that runs is not started a second time.
	/// </summary>
	[Test]
	public async Task ApplySettingsAsync_Keeps_A_Running_Clipboard_History()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		settings.TrackClipboardHistory = true;

		IClipboardLogRunner clipboardLog = Substitute.For<IClipboardLogRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			clipboardLog
				.IsRunning
				.Returns(true);

			builder.RegisterInstance(clipboardLog);
		});

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		await sut.ApplySettingsAsync(
			settings,
			wasPersistingClipboard: false,
			previousAutoLockMinutes: settings.AutoLockMinutes);

		// Assert
		await clipboardLog
			.DidNotReceiveWithAnyArgs()
			.StartAsync();
	}

	/// <summary>
	/// <see cref="BackgroundServices.ApplySettingsAsync" />: the hotkey tracking stays off when the saved settings turn it
	/// off.
	/// </summary>
	[Test]
	public async Task ApplySettingsAsync_Leaves_The_Hotkey_Tracking_Off_When_It_Is_Off()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings(trackHotkeys: false);

		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(hook));

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		await sut.ApplySettingsAsync(
			settings,
			wasPersistingClipboard: false,
			previousAutoLockMinutes: settings.AutoLockMinutes);

		// Assert
		await hook
			.DidNotReceiveWithAnyArgs()
			.StartTrackingAsync();
	}

	/// <summary>
	/// <see cref="BackgroundServices.PauseHotkeyTrackingAsync()" />: a hook that does not run is not stopped.
	/// </summary>
	[Test]
	public async Task PauseHotkeyTrackingAsync_Leaves_A_Hook_That_Does_Not_Run_Alone()
	{
		// Arrange
		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The hook is a lazy singleton that has already been used and then stopped.
			Lazy<IKeyboardInputHook> lazyHook = new(() => hook);

			_ = lazyHook.Value;

			builder.RegisterInstance(lazyHook);
		});

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		await sut.PauseHotkeyTrackingAsync();

		// Assert
		await hook
			.DidNotReceiveWithAnyArgs()
			.StopTrackingAsync();
	}

	/// <summary>
	/// <see cref="BackgroundServices.ResumeHotkeyTracking" />: the tracking stays off when the settings turn it off.
	/// </summary>
	[Test]
	public void ResumeHotkeyTracking_Leaves_The_Tracking_Off_When_It_Is_Off()
	{
		// Arrange
		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings(trackHotkeys: false));

			builder.RegisterInstance(hook);

			builder.RegisterInstance(settingsStore);
		});

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		sut.ResumeHotkeyTracking();

		// Assert
		hook
			.DidNotReceiveWithAnyArgs()
			.StartTrackingAsync();
	}

	/// <summary>
	/// <see cref="BackgroundServices.Start" />: the hotkey tracking starts with the token of the start.
	/// </summary>
	[Test]
	public void Start_Hands_Its_Token_To_The_Hotkey_Tracking()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings(trackHotkeys: true));

			builder.RegisterInstance(hook);

			builder.RegisterInstance(settingsStore);
		});

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		sut.Start(cancellation.Token);

		// Assert
		hook
			.Received(1)
			.StartTrackingAsync(cancellation.Token);
	}

	/// <summary>
	/// <see cref="BackgroundServices.Start" />: the hotkey tracking stays off when the settings turn it off.
	/// </summary>
	[Test]
	public void Start_Leaves_The_Hotkey_Tracking_Off_When_It_Is_Off()
	{
		// Arrange
		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings(trackHotkeys: false));

			builder.RegisterInstance(hook);

			builder.RegisterInstance(settingsStore);
		});

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		sut.Start();

		// Assert
		hook
			.DidNotReceiveWithAnyArgs()
			.StartTrackingAsync();
	}

	/// <summary>
	/// <see cref="BackgroundServices.Start" />: the hotkey tracking starts when the settings turn it on.
	/// </summary>
	[Test]
	public void Start_Starts_The_Hotkey_Tracking_When_It_Is_On()
	{
		// Arrange
		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings(trackHotkeys: true));

			builder.RegisterInstance(hook);

			builder.RegisterInstance(settingsStore);
		});

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		sut.Start();

		// Assert
		hook
			.Received(1)
			.StartTrackingAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="BackgroundServices.StopAsync" />: a clipboard history that does not run is not stopped.
	/// </summary>
	[Test]
	public async Task StopAsync_Leaves_A_Clipboard_History_That_Does_Not_Run_Alone()
	{
		// Arrange
		IClipboardLogRunner clipboardLog = Substitute.For<IClipboardLogRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboardLog));

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		await sut.StopAsync();

		// Assert
		clipboardLog
			.DidNotReceive()
			.Stop();
	}

	/// <summary>
	/// <see cref="BackgroundServices.StopAsync" />: a running clipboard history is stopped.
	/// </summary>
	[Test]
	public async Task StopAsync_Stops_A_Running_Clipboard_History()
	{
		// Arrange
		IClipboardLogRunner clipboardLog = Substitute.For<IClipboardLogRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			clipboardLog
				.IsRunning
				.Returns(true);

			builder.RegisterInstance(clipboardLog);
		});

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		await sut.StopAsync();

		// Assert
		clipboardLog
			.Received(1)
			.Stop();
	}

	/// <summary>
	/// <see cref="BackgroundServices.StopAsync" />: a running hotkey tracking is stopped.
	/// </summary>
	[Test]
	public async Task StopAsync_Stops_A_Running_Hotkey_Tracking()
	{
		// Arrange
		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			hook
				.IsRunning
				.Returns(true);

			// The hook is a lazy singleton that has already been used.
			Lazy<IKeyboardInputHook> lazyHook = new(() => hook);

			_ = lazyHook.Value;

			builder.RegisterInstance(lazyHook);
		});

		BackgroundServices sut = mock.Create<BackgroundServices>();

		// Act
		await sut.StopAsync();

		// Assert
		await hook
			.Received(1)
			.StopTrackingAsync(Arg.Any<CancellationToken>());
	}
	#endregion

}
