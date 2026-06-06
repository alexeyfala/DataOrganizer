using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using DataOrganizer.Services;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardHistoryPersistenceCoordinator)}"" type")]
internal class ClipboardHistoryPersistenceCoordinatorTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ClipboardHistoryPersistenceCoordinator.DisablePersistence" />.
	/// </summary>
	[Test]
	public void DisablePersistence_Erases_All()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		ClipboardHistoryPersistenceCoordinator sut = CreateSut(Settings(persist: true), Substitute.For<IClipboardHistoryService>(), store);

		// Act
		sut.DisablePersistence();

		// Assert
		store
			.Received()
			.EraseAll();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryPersistenceCoordinator.DisposeAsync" />: a final save flushes when unlocked.
	/// </summary>
	[Test]
	public async Task DisposeAsync_When_Unlocked_Flushes()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(true);

		IClipboardHistoryService service = Substitute.For<IClipboardHistoryService>();

		service.Entries.Returns(new ObservableCollection<ClipboardHistoryEntryBase> { TextEntry("a", [1]) });

		ClipboardHistoryPersistenceCoordinator sut = CreateSut(Settings(persist: true), service, store);

		// Act
		await sut.DisposeAsync();

		// Assert
		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryPersistenceCoordinator.Receive" />: an explicit clear erases the journal.
	/// </summary>
	[Test]
	public void Receive_ClearedByUser_Erases_History()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(true);

		ClipboardHistoryPersistenceCoordinator sut = CreateSut(Settings(persist: true), Substitute.For<IClipboardHistoryService>(), store);

		// Act
		sut.Receive(new ClipboardHistoryChangedMessage(ClipboardHistoryChangeKind.ClearedByUser));

		// Assert
		store
			.Received()
			.EraseHistory();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryPersistenceCoordinator.Receive" />: a tracking-off clear keeps the journal.
	/// </summary>
	[Test]
	public void Receive_ClearedForStop_Keeps_History()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(true);

		ClipboardHistoryPersistenceCoordinator sut = CreateSut(Settings(persist: true), Substitute.For<IClipboardHistoryService>(), store);

		// Act
		sut.Receive(new ClipboardHistoryChangedMessage(ClipboardHistoryChangeKind.ClearedForStop));

		// Assert
		store
			.DidNotReceive()
			.EraseHistory();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryPersistenceCoordinator.RequiresUnlock" />.
	/// </summary>
	[Test]
	public void RequiresUnlock_Reflects_Settings_And_Store_State()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(false);

		ClipboardHistoryPersistenceCoordinator unlockable = CreateSut(Settings(persist: true), Substitute.For<IClipboardHistoryService>(), store);

		ClipboardHistoryPersistenceCoordinator persistenceOff = CreateSut(Settings(persist: false), Substitute.For<IClipboardHistoryService>(), store);

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
	/// Test of <see cref="ClipboardHistoryPersistenceCoordinator.TryUnlockAndMergeAsync" />: merges and saves.
	/// </summary>
	[Test]
	public async Task TryUnlockAndMerge_Merges_And_Saves()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(true);

		ClipboardHistoryEntryBase[] loaded = [TextEntry("A", [1])];

		store
			.TryUnlockAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
			.Returns(new ClipboardHistoryUnlockResult(ClipboardHistoryUnlockStatus.Unlocked, loaded));

		IClipboardHistoryService service = Substitute.For<IClipboardHistoryService>();

		service.Entries.Returns(new ObservableCollection<ClipboardHistoryEntryBase>());

		ClipboardHistoryPersistenceCoordinator sut = CreateSut(Settings(persist: true), service, store);

		// Act
		ClipboardHistoryUnlockStatus status = await sut.TryUnlockAndMergeAsync(Password("pw"));

		// Assert
		status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.Unlocked);

		service
			.Received()
			.Merge(loaded);

		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryPersistenceCoordinator.TryUnlockAndMergeAsync" />: wrong password is a no-op.
	/// </summary>
	[Test]
	public async Task TryUnlockAndMerge_Wrong_Password_Does_Not_Merge_Or_Save()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store
			.TryUnlockAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
			.Returns(new ClipboardHistoryUnlockResult(ClipboardHistoryUnlockStatus.WrongPassword, []));

		IClipboardHistoryService service = Substitute.For<IClipboardHistoryService>();

		ClipboardHistoryPersistenceCoordinator sut = CreateSut(Settings(persist: true), service, store);

		// Act
		ClipboardHistoryUnlockStatus status = await sut.TryUnlockAndMergeAsync(Password("wrong"));

		// Assert
		status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.WrongPassword);

		service
			.DidNotReceive()
			.Merge(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>());

		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a coordinator with the supplied collaborators, a synchronous dispatcher and substituted logger / messenger.
	/// </summary>
	private static ClipboardHistoryPersistenceCoordinator CreateSut(
		IAppSettingsManager settingsManager,
		IClipboardHistoryService service,
		IClipboardHistoryStore store)
	{
		return new ClipboardHistoryPersistenceCoordinator(
			settingsManager,
			service,
			store,
			new InlineDispatcherAccessor(),
			Substitute.For<ILogger>(),
			Substitute.For<IMessenger>());
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
