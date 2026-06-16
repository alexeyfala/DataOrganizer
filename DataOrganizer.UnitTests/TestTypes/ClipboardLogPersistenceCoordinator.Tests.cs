using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogPersistenceCoordinator)}"" type")]
internal class ClipboardLogPersistenceCoordinatorTests
{
	#region Methods
	/// <summary>
	/// Test that an explicit clear cancels a pending debounced save and erases the journal.
	/// </summary>
	[Test]
	public async Task ClearedByUser_Cancels_Pending_Save()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(true);

		IClipboardLogService log = Substitute.For<IClipboardLogService>();

		log.Entries.Returns([TextEntry("a", [1])]);

		ClipboardLogPersistenceCoordinator sut = CreateSut(
			Settings(persist: true),
			log,
			store,
			new WeakReferenceMessenger(),
			SaveDebounce);

		// Act
		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.ClearedByUser));

		await Task.Delay(SaveSettleDelay);

		// Assert
		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());

		store
			.Received()
			.EraseHistory();
	}

	/// <summary>
	/// Test that a burst of change notifications is coalesced by the debounce into a single save.
	/// </summary>
	[Test]
	public async Task Debounce_Coalesces_Burst_Into_Single_Save()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(true);

		IClipboardLogService log = Substitute.For<IClipboardLogService>();

		log.Entries.Returns([TextEntry("a", [1])]);

		ClipboardLogPersistenceCoordinator sut = CreateSut(Settings(persist: true), log, store, messenger, SaveDebounce);

		sut.Start();

		// Act (three rapid changes — each cancels the previous pending save).
		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		await Task.Delay(SaveSettleDelay);

		// Assert
		await store
			.Received(1)
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.DisablePersistence" />: it erases all persisted state.
	/// </summary>
	[Test]
	public void DisablePersistence_Erases_All()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		using AutoMock mock = CreateMock(Settings(persist: true), Substitute.For<IClipboardLogService>(), store);

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
		IMessenger messenger = new WeakReferenceMessenger();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(true);

		IClipboardLogService log = Substitute.For<IClipboardLogService>();

		log.Entries.Returns([TextEntry("a", [1])]);

		ClipboardLogPersistenceCoordinator sut = CreateSut(Settings(persist: true), log, store, messenger, SaveDebounce);

		sut.Start();

		await sut.DisposeAsync();

		// Act (a change after dispose must not be handled).
		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		await Task.Delay(SaveSettleDelay);

		// Assert (the single save is the dispose-time flush; the post-dispose change added none).
		await store
			.Received(1)
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.DisposeAsync" />: a locked store is not saved on flush.
	/// </summary>
	[Test]
	public async Task DisposeAsync_When_Locked_Does_Not_Save()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(false);

		using AutoMock mock = CreateMock(Settings(persist: true), Substitute.For<IClipboardLogService>(), store);

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		await sut.DisposeAsync();

		// Assert
		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.DisposeAsync" />: a final save flushes when unlocked.
	/// </summary>
	[Test]
	public async Task DisposeAsync_When_Unlocked_Flushes()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(true);

		IClipboardLogService log = Substitute.For<IClipboardLogService>();

		log.Entries.Returns([TextEntry("a", [1])]);

		using AutoMock mock = CreateMock(Settings(persist: true), log, store);

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		await sut.DisposeAsync();

		// Assert
		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.Receive" />: an explicit clear erases the journal.
	/// </summary>
	[Test]
	public void Receive_ClearedByUser_Erases_History()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(true);

		using AutoMock mock = CreateMock(Settings(persist: true), Substitute.For<IClipboardLogService>(), store);

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
	public void Receive_ClearedForStop_Keeps_History()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(true);

		using AutoMock mock = CreateMock(Settings(persist: true), Substitute.For<IClipboardLogService>(), store);

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.ClearedForStop));

		// Assert
		store
			.DidNotReceive()
			.EraseHistory();
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.RequiresUnlock" />: it is true only when persistence is on and the store is locked.
	/// </summary>
	[Test]
	public void RequiresUnlock_Reflects_Settings_And_Store_State()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(false);

		using AutoMock unlockableMock = CreateMock(Settings(persist: true), Substitute.For<IClipboardLogService>(), store);

		ClipboardLogPersistenceCoordinator unlockable = unlockableMock.Create<ClipboardLogPersistenceCoordinator>();

		using AutoMock persistenceOffMock = CreateMock(Settings(persist: false), Substitute.For<IClipboardLogService>(), store);

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
		// Arrange (a real messenger would throw on a duplicate registration).
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(
			Settings(persist: true),
			Substitute.For<IClipboardLogService>(),
			Substitute.For<IClipboardLogStore>(),
			messenger);

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
	public async Task TryUnlockAndMerge_Merges_And_Saves()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(true);

		ClipboardHistoryEntryBase[] loaded = [TextEntry("A", [1])];

		store
			.TryUnlockAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
			.Returns(new ClipboardLogUnlockResult(ClipboardHistoryLogStatus.Unlocked, loaded));

		IClipboardLogService log = Substitute.For<IClipboardLogService>();

		log.Entries.Returns([]);

		using AutoMock mock = CreateMock(Settings(persist: true), log, store);

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		ClipboardHistoryLogStatus status = await sut.TryUnlockAndMergeAsync(Password("pw"));

		// Assert
		status
			.Should()
			.Be(ClipboardHistoryLogStatus.Unlocked);

		log
			.Received()
			.Merge(loaded);

		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardLogPersistenceCoordinator.TryUnlockAndMergeAsync" />: wrong password is a no-op.
	/// </summary>
	[Test]
	public async Task TryUnlockAndMerge_Wrong_Password_Does_Not_Merge_Or_Save()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store
			.TryUnlockAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
			.Returns(new ClipboardLogUnlockResult(ClipboardHistoryLogStatus.WrongPassword, []));

		IClipboardLogService log = Substitute.For<IClipboardLogService>();

		using AutoMock mock = CreateMock(Settings(persist: true), log, store);

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		ClipboardHistoryLogStatus status = await sut.TryUnlockAndMergeAsync(Password("wrong"));

		// Assert
		status
			.Should()
			.Be(ClipboardHistoryLogStatus.WrongPassword);

		log
			.DidNotReceive()
			.Merge(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>());

		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test that a change notification, while locked, schedules no save.
	/// </summary>
	[Test]
	public async Task Updated_When_Locked_Does_Not_Save()
	{
		// Arrange
		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(false);

		using AutoMock mock = CreateMock(Settings(persist: true), Substitute.For<IClipboardLogService>(), store);

		ClipboardLogPersistenceCoordinator sut = mock.Create<ClipboardLogPersistenceCoordinator>();

		// Act
		sut.Receive(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		await Task.Delay(SaveSettleDelay);

		// Assert
		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test that a change notification, while unlocked, triggers a debounced save through the messenger.
	/// </summary>
	[Test]
	public async Task Updated_When_Unlocked_Saves_After_Debounce()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		IClipboardLogStore store = Substitute.For<IClipboardLogStore>();

		store.IsUnlocked.Returns(true);

		IClipboardLogService log = Substitute.For<IClipboardLogService>();

		log.Entries.Returns([TextEntry("a", [1])]);

		ClipboardLogPersistenceCoordinator sut = CreateSut(Settings(persist: true), log, store, messenger, SaveDebounce);

		sut.Start();

		// Act
		messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		await Task.Delay(SaveSettleDelay);

		// Assert
		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Short debounce used by timing-sensitive tests so a scheduled save fires quickly.
	/// </summary>
	private static readonly TimeSpan SaveDebounce = TimeSpan.FromMilliseconds(30.0);

	/// <summary>
	/// Wait long enough for a debounced save to either fire or be proven cancelled.
	/// </summary>
	private static readonly TimeSpan SaveSettleDelay = TimeSpan.FromMilliseconds(250.0);

	/// <summary>
	/// Builds an auto-mock container with the supplied collaborators, a synchronous dispatcher and an
	/// optional messenger (a substitute when none is supplied); the logger is auto-mocked.
	/// </summary>
	private static AutoMock CreateMock(
		IAppSettingsManager settingsManager,
		IClipboardLogService log,
		IClipboardLogStore store,
		IMessenger? messenger = null)
	{
		return AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(settingsManager);

			builder.RegisterInstance(log);

			builder.RegisterInstance(store);

			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger ?? Substitute.For<IMessenger>());

			// The coordinator is IAsyncDisposable; let the test own its lifetime so AutoMock's
			// synchronous scope disposal does not try (and fail) to dispose it.
			builder
				.RegisterType<ClipboardLogPersistenceCoordinator>()
				.ExternallyOwned();
		});
	}

	/// <summary>
	/// Builds a coordinator directly with an explicit messenger and debounce delay (for the timing tests,
	/// where AutoMock cannot inject a custom debounce through the public constructor).
	/// </summary>
	private static ClipboardLogPersistenceCoordinator CreateSut(
		IAppSettingsManager settingsManager,
		IClipboardLogService log,
		IClipboardLogStore store,
		IMessenger messenger,
		TimeSpan saveDebounce)
	{
		return new ClipboardLogPersistenceCoordinator(
			settingsManager,
			log,
			store,
			new InlineDispatcherAccessor(),
			Substitute.For<ILogger>(),
			messenger,
			saveDebounce);
	}

	/// <summary>
	/// UTF-8 password bytes.
	/// </summary>
	private static byte[] Password(string value) => TextHelper.Utf8Encoding.GetBytes(value);

	/// <summary>
	/// A settings manager whose <see cref="AppSettings.PersistClipboardHistory" /> equals <paramref name="persist" />.
	/// </summary>
	private static IAppSettingsManager Settings(bool persist)
	{
		IAppSettingsManager manager = Substitute.For<IAppSettingsManager>();

		manager.Settings.Returns(new AppSettings
		{
			Language = "en-us",
			PrimaryColor = default,
			SecondaryColor = default,
			Theme = default,
			PersistClipboardHistory = persist
		});

		return manager;
	}

	/// <summary>
	/// A minimal text entry with the given hash.
	/// </summary>
	private static ClipboardTextEntry TextEntry(string text, byte[] hash) => new()
	{
		Text = text,
		Html = null,
		Rtf = null,
		Hash = hash
	};
	#endregion
}
