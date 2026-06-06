using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardHistoryService)}"" type")]
internal class ClipboardHistoryServiceTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.ClearAsync" />: the journal is erased when unlocked.
	/// </summary>
	[Test]
	public async Task ClearAsync_When_Unlocked_Erases_History()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(true);

		using AutoMock mock = CreateMock(store, persist: true);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		sut.Entries.Add(TextEntry("a", [1]));

		// Act
		await sut.ClearAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		store
			.Received()
			.EraseHistory();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.ClearEntriesAsync" />: the journal is kept
	/// (a tracking toggle must not erase the saved history).
	/// </summary>
	[Test]
	public async Task ClearEntriesAsync_When_Unlocked_Keeps_History()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(true);

		using AutoMock mock = CreateMock(store, persist: true);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		sut.Entries.Add(TextEntry("a", [1]));

		// Act
		await sut.ClearEntriesAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		store
			.DidNotReceive()
			.EraseHistory();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.DisablePersistence" />.
	/// </summary>
	[Test]
	public void DisablePersistence_Erases_All()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		using AutoMock mock = CreateMock(store, persist: true);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		// Act
		sut.DisablePersistence();

		// Assert
		store
			.Received()
			.EraseAll();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.DisposeAsync" />: a final save flushes when unlocked.
	/// </summary>
	[Test]
	public async Task DisposeAsync_When_Unlocked_Flushes()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(true);

		using AutoMock mock = CreateMock(store, persist: true);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		sut.Entries.Add(TextEntry("a", [1]));

		// Act
		await sut.DisposeAsync();

		// Assert
		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.RequiresUnlock" />.
	/// </summary>
	[Test]
	public void RequiresUnlock_Reflects_Settings_And_Store_State()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(false);

		using AutoMock enabled = CreateMock(store, persist: true);

		ClipboardHistoryService unlockable = enabled.Create<ClipboardHistoryService>();

		using AutoMock disabled = CreateMock(store, persist: false);

		ClipboardHistoryService persistenceOff = disabled.Create<ClipboardHistoryService>();

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
	/// Test of <see cref="ClipboardHistoryService.TryUnlockAndMergeAsync" />: merges, dedupes and saves.
	/// </summary>
	[Test]
	public async Task TryUnlockAndMerge_Merges_Dedupes_And_Saves()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(true);

		ClipboardHistoryEntryBase[] loaded = [TextEntry("A", [1]), TextEntry("B", [2])];

		store
			.TryUnlockAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
			.Returns(new ClipboardHistoryUnlockResult(ClipboardHistoryUnlockStatus.Unlocked, loaded));

		using AutoMock mock = CreateMock(store, persist: true);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		// Current session entries: "C" duplicates loaded "B" by hash [2].
		sut.Entries.Add(TextEntry("C", [2]));

		sut.Entries.Add(TextEntry("D", [9]));

		// Act
		ClipboardHistoryUnlockStatus status = await sut.TryUnlockAndMergeAsync(Password("pw"));

		// Assert
		status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.Unlocked);

		sut.Entries
			.Cast<ClipboardTextEntry>()
			.Select(x => x.Text)
			.Should()
			.Equal("C", "D", "A");

		await store
			.Received()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.TryUnlockAndMergeAsync" />: a wrong password is a no-op.
	/// </summary>
	[Test]
	public async Task TryUnlockAndMerge_Wrong_Password_Does_Not_Merge_Or_Save()
	{
		// Arrange
		IClipboardHistoryStore store = Substitute.For<IClipboardHistoryStore>();

		store.IsUnlocked.Returns(false);

		store
			.TryUnlockAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
			.Returns(new ClipboardHistoryUnlockResult(ClipboardHistoryUnlockStatus.WrongPassword, []));

		using AutoMock mock = CreateMock(store, persist: true);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		sut.Entries.Add(TextEntry("E", [5]));

		// Act
		ClipboardHistoryUnlockStatus status = await sut.TryUnlockAndMergeAsync(Password("wrong"));

		// Assert
		status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.WrongPassword);

		sut.Entries
			.Should()
			.ContainSingle();

		await store
			.DidNotReceive()
			.SaveAsync(Arg.Any<IReadOnlyList<ClipboardHistoryEntryBase>>(), Arg.Any<CancellationToken>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds an auto-mock container wiring the supplied store, a synchronous dispatcher and a
	/// settings manager whose <see cref="AppSettings.PersistClipboardHistory" /> equals <paramref name="persist" />.
	/// </summary>
	private static AutoMock CreateMock(IClipboardHistoryStore store, bool persist)
	{
		return AutoMock.GetLoose(builder =>
		{
			IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

			settingsManager.Settings.Returns(new AppSettings
			{
				Language = "en-us",
				PrimaryColor = default,
				SecondaryColor = default,
				Theme = default,
				PersistClipboardHistory = persist
			});

			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(Substitute.For<IClipboardAccessor>());

			builder.RegisterInstance(Substitute.For<IStorageAccessor>());

			builder.RegisterInstance(settingsManager);

			builder.RegisterInstance(store);
		});
	}

	/// <summary>
	/// UTF-8 password bytes.
	/// </summary>
	private static byte[] Password(string value) => TextHelper.Utf8Encoding.GetBytes(value);

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
