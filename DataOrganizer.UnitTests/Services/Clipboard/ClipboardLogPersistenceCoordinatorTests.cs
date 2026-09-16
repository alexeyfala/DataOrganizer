using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Clipboard;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Messages.Clipboard;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogPersistenceCoordinator)}"" type")]
internal class ClipboardLogPersistenceCoordinatorTests
{
	#region Data
	/// <summary>
	/// Constructor finder that also sees the internal test constructor taking the debounce.
	/// </summary>
	private static readonly DefaultConstructorFinder DebounceConstructor = new(static type => type.GetConstructors(
		BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

	/// <summary>
	/// Short debounce used by timing-sensitive tests so a scheduled save fires quickly.
	/// </summary>
	private static readonly TimeSpan SaveDebounce = TimeSpan.FromMilliseconds(30.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.DisablePersistence" />: it erases all persisted state.
	/// </summary>
	[Test]
	public void DisablePersistence_Erases_All()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		sut.DisablePersistence();

		// Assert
		store
			.Received()
			.EraseAll();
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.DisposeAsync" />: it unsubscribes, so a later
	/// change raises no further save (the only save is the dispose-time flush).
	/// </summary>
	[Test]
	public async Task DisposeAsync_Unsubscribes_From_Further_Changes()
	{
		// Arrange
		FakeTimeProvider time = new();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			clipboardLog
				.Entries
				.Returns([ClipboardEntryFactory.CreateTextEntry("a", [1])]);

			store
				.IsUnlocked
				.Returns(true);

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(call => scheduled.Add(call.Arg<Task>()));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(store);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.FindConstructorsWith(DebounceConstructor)
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>(
			TypedParameter.From(SaveDebounce));

		sut.Start();

		await sut.DisposeAsync();

		// Act (a change after dispose must not be handled).
		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		time.Advance(SaveDebounce);

		await Task.WhenAll([.. scheduled]);

		// Assert (the single save is the dispose-time flush; the post-dispose change added none).
		await store
			.Received(1)
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.DisposeAsync" />: a locked store is not saved on flush.
	/// </summary>
	[Test]
	public async Task DisposeAsync_When_Locked_Does_Not_Save()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			store
				.IsUnlocked
				.Returns(false);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		await sut.DisposeAsync();

		// Assert
		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.DisposeAsync" />: a final save flushes when unlocked.
	/// </summary>
	[Test]
	public async Task DisposeAsync_When_Unlocked_Flushes()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			clipboardLog
				.Entries
				.Returns([ClipboardEntryFactory.CreateTextEntry("a", [1])]);

			store
				.IsUnlocked
				.Returns(true);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		await sut.DisposeAsync();

		// Assert
		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.Receive" />: an explicit clear cancels a pending debounced save and erases the journal.
	/// </summary>
	[Test]
	public async Task Receive_ClearedByUser_Cancels_Pending_Save()
	{
		// Arrange
		FakeTimeProvider time = new();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			clipboardLog
				.Entries
				.Returns([ClipboardEntryFactory.CreateTextEntry("a", [1])]);

			store
				.IsUnlocked
				.Returns(true);

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(call => scheduled.Add(call.Arg<Task>()));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(store);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.FindConstructorsWith(DebounceConstructor)
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>(
			TypedParameter.From(SaveDebounce));

		// Act
		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.ClearedByUser));

		time.Advance(SaveDebounce);

		await Task.WhenAll([.. scheduled]);

		// Assert
		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());

		store
			.Received()
			.EraseHistory();
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.Receive" />: an explicit clear erases the journal.
	/// </summary>
	[Test]
	public void Receive_ClearedByUser_Erases_History()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			store
				.IsUnlocked
				.Returns(true);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.ClearedByUser));

		// Assert
		store
			.Received()
			.EraseHistory();
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.Receive" />: a tracking-off clear keeps the journal.
	/// </summary>
	[Test]
	public void Receive_ClearedOnStop_Keeps_History()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			store
				.IsUnlocked
				.Returns(true);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.ClearedOnStop));

		// Assert
		store
			.DidNotReceive()
			.EraseHistory();
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.Receive" />: a burst of change notifications is coalesced by the debounce into a single save.
	/// </summary>
	[Test]
	public async Task Receive_Updated_Coalesces_Burst_Into_Single_Save()
	{
		// Arrange
		FakeTimeProvider time = new();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			clipboardLog
				.Entries
				.Returns([ClipboardEntryFactory.CreateTextEntry("a", [1])]);

			store
				.IsUnlocked
				.Returns(true);

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(call => scheduled.Add(call.Arg<Task>()));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(store);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.FindConstructorsWith(DebounceConstructor)
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>(
			TypedParameter.From(SaveDebounce));

		sut.Start();

		// Act (three rapid changes — each cancels the previous pending save).
		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		time.Advance(SaveDebounce);

		await Task.WhenAll([.. scheduled]);

		// Assert
		await store
			.Received(1)
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.Receive" />: a change notification, while locked, schedules no save.
	/// </summary>
	[Test]
	public async Task Receive_Updated_When_Locked_Does_Not_Save()
	{
		// Arrange
		FakeTimeProvider time = new();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			store
				.IsUnlocked
				.Returns(false);

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(call => scheduled.Add(call.Arg<Task>()));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(store);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.FindConstructorsWith(DebounceConstructor)
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>(
			TypedParameter.From(SaveDebounce));

		// Act
		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		time.Advance(SaveDebounce);

		await Task.WhenAll([.. scheduled]);

		// Assert
		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.Receive" />: a change notification, while unlocked, triggers a debounced save.
	/// </summary>
	[Test]
	public async Task Receive_Updated_When_Unlocked_Saves_After_Debounce()
	{
		// Arrange
		FakeTimeProvider time = new();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			clipboardLog
				.Entries
				.Returns([ClipboardEntryFactory.CreateTextEntry("a", [1])]);

			store
				.IsUnlocked
				.Returns(true);

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(call => scheduled.Add(call.Arg<Task>()));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(store);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.FindConstructorsWith(DebounceConstructor)
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>(
			TypedParameter.From(SaveDebounce));

		sut.Start();

		// Act
		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		time.Advance(SaveDebounce);

		await Task.WhenAll([.. scheduled]);

		// Assert
		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.RequiresUnlock" />: it is true only when persistence is on and the store is locked.
	/// </summary>
	[Test]
	public void RequiresUnlock_Reflects_Settings_And_Store_State()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock unlockableMock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			store
				.IsUnlocked
				.Returns(false);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator unlockable = unlockableMock.Create<ClipboardLogPersistenceCoordinator>();

		using AutoMock persistenceOffMock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = false
				});

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator persistenceOff = persistenceOffMock.Create<ClipboardLogPersistenceCoordinator>();

		// Act, Assert
		unlockable.RequiresUnlock
			.Should()
			.BeTrue();

		persistenceOff.RequiresUnlock
			.Should()
			.BeFalse();

		store.IsUnlocked.Returns(true);

		unlockable.RequiresUnlock
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.Start" />: calling it twice does not re-subscribe.
	/// </summary>
	[Test]
	public void Start_Is_Idempotent()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			// A real messenger would throw on a duplicate registration.
			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		Action act = () =>
		{
			sut.Start();

			sut.Start();
		};

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.TryUnlockAndMergeAsync" />: merges and saves.
	/// </summary>
	[Test]
	public async Task TryUnlockAndMergeAsync_Merges_And_Saves()
	{
		// Arrange
		ClipboardLogEntryBase[] loaded = [ClipboardEntryFactory.CreateTextEntry("A", [1])];

		IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			clipboardLog
				.Entries
				.Returns([]);

			store
				.IsUnlocked
				.Returns(true);

			store
				.TryUnlockAsync(Arg.Any<PinnedBuffer>(), Arg.Any<CancellationToken>())
				.Returns(new ClipboardLogUnlockResult(ClipboardLogStatus.Unlocked, loaded));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		ClipboardLogStatus status = await sut.TryUnlockAndMergeAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		status
			.Should()
			.Be(ClipboardLogStatus.Unlocked);

		clipboardLog
			.Received()
			.Merge(loaded);

		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.TryUnlockAndMergeAsync" />: wrong password is a no-op.
	/// </summary>
	[Test]
	public async Task TryUnlockAndMergeAsync_Wrong_Password_Does_Not_Merge_Or_Save()
	{
		// Arrange
		IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					PersistClipboardHistory = true
				});

			store
				.TryUnlockAsync(Arg.Any<PinnedBuffer>(), Arg.Any<CancellationToken>())
				.Returns(new ClipboardLogUnlockResult(ClipboardLogStatus.WrongPassword, []));

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(store);

			builder.RegisterInstance(TimeProvider.System);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		ClipboardLogStatus status = await sut.TryUnlockAndMergeAsync(SecretFactory.CreatePassword("wrong"));

		// Assert
		status
			.Should()
			.Be(ClipboardLogStatus.WrongPassword);

		clipboardLog
			.DidNotReceive()
			.Merge(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>());

		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardLogEntryBase>>(), Arg.Any<CancellationToken>());
	}
	#endregion
}
