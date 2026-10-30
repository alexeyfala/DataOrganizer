using AppInstance.Interfaces;
using AppLifecycle.Services;
using Autofac;
using Autofac.Extras.Moq;
using Clipboard.Interfaces;
using DataProtection.Interfaces;
using GlobalHotkeys.Interfaces;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using Settings.Dto;
using Settings.Interfaces;
using Settings.Services;
using Settings.UnitTests.Factories;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppLifecycle.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsSession)}"" type")]
internal class SettingsSessionTests
{
	#region Methods
	/// <summary>
	/// <see cref="SettingsSession.EndAsync" />: a changed auto-lock delay restarts the countdown at once.
	/// </summary>
	[Test]
	public async Task EndAsync_Applies_A_Changed_Auto_Lock_Delay()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(autoLock);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		mock
			.Create<ISessionHierarchy>()
			.Hierarchy
			.AddRange([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		sut.Begin().AutoLockMinutes = 5;

		sut.Save();

		// Act
		await sut.EndAsync();

		// Assert
		autoLock
			.Received(1)
			.Arm();
	}

	/// <summary>
	/// <see cref="SettingsSession.EndAsync" />: an unchanged auto-lock delay leaves the countdown running.
	/// </summary>
	[Test]
	public async Task EndAsync_Keeps_An_Unchanged_Auto_Lock_Delay()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings(autoLockMinutes: 5));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(autoLock);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		mock
			.Create<ISessionHierarchy>()
			.Hierarchy
			.AddRange([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		sut.Begin();

		sut.Save();

		// Act
		await sut.EndAsync();

		// Assert
		autoLock
			.DidNotReceive()
			.Arm();

		autoLock
			.DidNotReceive()
			.Stop();
	}

	/// <summary>
	/// <see cref="SettingsSession.EndAsync" />: saved settings are written and the hotkey tracking restarts, while settings
	/// closed without saving roll the previewed theme back.
	/// </summary>
	[Test]
	public async Task EndAsync_Saves_And_Restarts_The_Hotkeys_Or_Rolls_The_Theme_Back([Values] bool isSave)
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The hook is a lazy singleton that an earlier window has already used.
			Lazy<IKeyboardInputHook> lazyHook = new(() => hook);

			_ = lazyHook.Value;

			hook
				.IsRunning
				.Returns(true);

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings(trackHotkeys: true));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(themeService);

			builder.RegisterInstance(lazyHook);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		sut.Begin();

		if (isSave)
		{
			sut.Save();
		}

		// Act
		await sut.EndAsync();

		// Assert
		if (isSave)
		{
			await hook
				.Received(1)
				.StopTrackingAsync(Arg.Any<CancellationToken>());

			await hook
				.Received(1)
				.StartTrackingAsync(Arg.Any<CancellationToken>());

			settingsStore
				.Received(1)
				.Overwrite(Arg.Any<AppSettings>());

			settingsStore
				.Received(1)
				.Save();
		}
		else
		{
			themeService
				.Received(1)
				.ApplyFromSettings();

			settingsStore
				.DidNotReceive()
				.Save();
		}
	}

	/// <summary>
	/// <see cref="SettingsSession.EndAsync" />: a saved setting that turns the clipboard history on starts it.
	/// </summary>
	[Test]
	public async Task EndAsync_Starts_The_Clipboard_History_Turned_On()
	{
		// Arrange
		IClipboardLogRunner clipboardLog = Substitute.For<IClipboardLogRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		sut.Begin().TrackClipboardHistory = true;

		sut.Save();

		// Act
		await sut.EndAsync();

		// Assert
		await clipboardLog
			.Received(1)
			.StartAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="SettingsSession.EndAsync" />: a saved setting that turns the saving of the clipboard history off stops
	/// the saving.
	/// </summary>
	[Test]
	public async Task EndAsync_Stops_Saving_The_Clipboard_History_Turned_Off()
	{
		// Arrange
		IClipboardLogPersistenceCoordinator clipboardLogPersistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			AppSettings previous = SettingsFactory.CreateSettings();

			previous.PersistClipboardHistory = true;

			previous.TrackClipboardHistory = true;

			settingsStore
				.Settings
				.Returns(previous);

			builder.RegisterInstance(clipboardLogPersistence);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		sut.Begin().PersistClipboardHistory = false;

		sut.Save();

		// Act
		await sut.EndAsync();

		// Assert
		clipboardLogPersistence
			.Received(1)
			.DisablePersistence();
	}

	/// <summary>
	/// <see cref="SettingsSession.EndAsync" />: a saved setting that turns the clipboard history off stops it and drops its
	/// entries.
	/// </summary>
	[Test]
	public async Task EndAsync_Stops_The_Clipboard_History_Turned_Off()
	{
		// Arrange
		IClipboardLogRunner clipboardLog = Substitute.For<IClipboardLogRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			AppSettings previous = SettingsFactory.CreateSettings();

			previous.TrackClipboardHistory = true;

			clipboardLog
				.IsRunning
				.Returns(true);

			settingsStore
				.Settings
				.Returns(previous);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		sut.Begin().TrackClipboardHistory = false;

		sut.Save();

		// Act
		await sut.EndAsync();

		// Assert
		clipboardLog
			.Received(1)
			.Stop();

		await clipboardLog
			.Received(1)
			.ClearEntriesAsync();
	}
	#endregion
}
