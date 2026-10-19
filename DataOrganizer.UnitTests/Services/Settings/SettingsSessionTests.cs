using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Runtime;
using DataOrganizer.Services.Settings;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Settings;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsSession)}"" type")]
internal class SettingsSessionTests
{
	#region Methods
	/// <summary>
	/// <see cref="SettingsSession.Begin" />: a new session starts neither saved nor discarded, whatever the last one ended
	/// with.
	/// </summary>
	[Test]
	public void Begin_Starts_Neither_Saved_Nor_Discarded()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		sut.Begin();

		sut.Save();

		sut.Discard();

		// Act
		sut.Begin();

		// Assert
		bool[] flags = [sut.IsSaved, sut.IsDiscarded];

		flags
			.Should()
			.OnlyContain(static x => !x);
	}

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

	/// <summary>
	/// <see cref="SettingsSession.ShouldConfirmClose" />: edits that are neither saved nor discarded ask for a confirmation,
	/// saved or discarded ones and unchanged settings do not.
	/// </summary>
	[Test]
	[TestCase(true, false, false, ExpectedResult = true)]
	[TestCase(true, true, false, ExpectedResult = false)]
	[TestCase(true, false, true, ExpectedResult = false)]
	[TestCase(false, false, false, ExpectedResult = false)]
	public bool ShouldConfirmClose_Asks_Only_For_Pending_Edits(bool isEdited, bool isSaved, bool isDiscarded)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		AppSettings draft = sut.Begin();

		if (isEdited)
		{
			draft.CheckForUpdates = !draft.CheckForUpdates;
		}

		if (isSaved)
		{
			sut.Save();
		}

		if (isDiscarded)
		{
			sut.Discard();
		}

		// Act
		return sut.ShouldConfirmClose;
	}
	#endregion
}
