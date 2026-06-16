using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Clipboard.Persistence;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using Shared.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogStore)}"" type")]
internal class ClipboardLogStoreTests
{
	#region Data
	private static readonly string BinPath = Path.Combine("clip", "History.bin");

	private static readonly string KeyPath = Path.Combine("clip", "History.key");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogStore.EraseAll" />: removes both journal and key files and locks the store.
	/// </summary>
	[Test]
	public async Task EraseAll_Removes_Both_Files_And_Locks()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		await sut.TryUnlockAsync(Password("pw"));

		await sut.SaveAsync([TextEntry("data")]);

		// Act
		sut.EraseAll();

		// Assert
		sut.IsUnlocked
			.Should()
			.BeFalse();

		sut.KeyFileExists
			.Should()
			.BeFalse();

		files.Files
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.EraseHistory" />: removes the journal but keeps the key and stays unlocked.
	/// </summary>
	[Test]
	public async Task EraseHistory_Removes_Journal_But_Keeps_Key()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		await sut.TryUnlockAsync(Password("pw"));

		await sut.SaveAsync([TextEntry("data")]);

		// Act
		sut.EraseHistory();

		// Assert
		sut.IsUnlocked
			.Should()
			.BeTrue();

		files.Files
			.Should()
			.ContainKey(KeyPath);

		files.Files
			.Should()
			.NotContainKey(BinPath);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.LoadEntriesAsync" />: an unsupported schema version is treated as empty.
	/// </summary>
	[Test]
	public async Task LoadEntries_With_Unsupported_Version_Returns_Empty()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		IEncryptionService encryption = mock.Create<IEncryptionService>();

		byte[] dek = encryption.CreateRandomDek();

		byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(new PersistedClipboardHistory
		{
			Version = PersistedClipboardHistory.CurrentVersion + 1
		});

		files.Files[BinPath] = encryption.EncryptWithDek(plaintext, dek)!;

		// Act
		IReadOnlyList<ClipboardHistoryEntryBase> result = await sut.LoadEntriesAsync(dek, default);

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.SaveAsync" /> / <see cref="ClipboardLogStore.TryUnlockAsync" />: a saved entry is restored after unlocking in a new session.
	/// </summary>
	[Test]
	public async Task Save_Then_Unlock_In_New_Session_Restores_Entries()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = CreateMock(files))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(Password("pw"));

			await writer.SaveAsync([TextEntry("secret")]);
		}

		// Act
		using AutoMock second = CreateMock(files);

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardHistoryUnlockResult result = await reader.TryUnlockAsync(Password("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryLogStatus.Unlocked);

		ClipboardTextEntry restored = result
			.Entries
			.Should()
			.ContainSingle()
			.Subject
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Subject;

		restored.Text
			.Should()
			.Be("secret");
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.SaveAsync" />: a later save replaces the previous journal.
	/// </summary>
	[Test]
	public async Task Save_Twice_Overwrites_Previous_Journal()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = CreateMock(files))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(Password("pw"));

			await writer.SaveAsync([TextEntry("old")]);

			await writer.SaveAsync([TextEntry("new")]);
		}

		// Act
		using AutoMock second = CreateMock(files);

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardHistoryUnlockResult result = await reader.TryUnlockAsync(Password("pw"));

		// Assert
		ClipboardTextEntry restored = result
			.Entries
			.Should()
			.ContainSingle()
			.Subject
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Subject;

		restored.Text
			.Should()
			.Be("new");
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.SaveAsync" />: an encryption failure writes no journal.
	/// </summary>
	[Test]
	public async Task Save_When_Encryption_Fails_Writes_Nothing()
	{
		// Arrange
		InMemoryFileSystem files = new();

		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		encryption
			.CreateRandomDek()
			.Returns(new byte[32]);

		// Wrapping the key succeeds (so the store unlocks)...
		encryption
			.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
			.Returns([1, 2, 3]);

		// ...but encrypting the journal fails.
		encryption
			.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>())
			.Returns((byte[]?)null);

		using AutoMock mock = CreateMock(files, encryption);

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		await sut.TryUnlockAsync(Password("pw"));

		// Act
		await sut.SaveAsync([TextEntry("data")]);

		// Assert
		files.Files
			.Should()
			.NotContainKey(BinPath);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.SaveAsync" />: writes nothing while locked.
	/// </summary>
	[Test]
	public async Task Save_Without_Unlock_Writes_Nothing()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		// Act
		await sut.SaveAsync([TextEntry("data")]);

		// Assert
		files.Files
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a new key is created when none exists.
	/// </summary>
	[Test]
	public async Task TryUnlock_Creates_Key_When_None_Exists()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		// Act
		ClipboardHistoryUnlockResult result = await sut.TryUnlockAsync(Password("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryLogStatus.Unlocked);

		result.Entries
			.Should()
			.BeEmpty();

		sut.IsUnlocked
			.Should()
			.BeTrue();

		sut.KeyFileExists
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a failure to wrap a new key yields Failed.
	/// </summary>
	[Test]
	public async Task TryUnlock_When_Key_Wrap_Fails_Returns_Failed()
	{
		// Arrange
		InMemoryFileSystem files = new();

		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		encryption
			.CreateRandomDek()
			.Returns(new byte[32]);

		encryption
			.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
			.Returns((byte[]?)null);

		using AutoMock mock = CreateMock(files, encryption);

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		// Act
		ClipboardHistoryUnlockResult result = await sut.TryUnlockAsync(Password("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryLogStatus.Failed);

		sut.IsUnlocked
			.Should()
			.BeFalse();

		files.Files
			.Should()
			.NotContainKey(KeyPath);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a corrupt journal yields no entries.
	/// </summary>
	[Test]
	public async Task TryUnlock_With_Corrupt_Journal_Returns_Empty()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = CreateMock(files))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(Password("pw"));

			await writer.SaveAsync([TextEntry("data")]);
		}

		files.Files[BinPath] = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

		// Act
		using AutoMock second = CreateMock(files);

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardHistoryUnlockResult result = await reader.TryUnlockAsync(Password("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryLogStatus.Unlocked);

		result.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a wrong password is rejected.
	/// </summary>
	[Test]
	public async Task TryUnlock_With_Wrong_Password_Returns_WrongPassword()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = CreateMock(files))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(Password("right"));
		}

		// Act
		using AutoMock second = CreateMock(files);

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardHistoryUnlockResult result = await reader.TryUnlockAsync(Password("wrong"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryLogStatus.WrongPassword);

		reader.IsUnlocked
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds an auto-mock container backed by the supplied in-memory file system and a real
	/// <see cref="EncryptionService" /> (its logger is auto-mocked).
	/// </summary>
	private static AutoMock CreateMock(InMemoryFileSystem files, IEncryptionService? encryption = null)
	{
		return AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetClipboardHistoryFilePath(Arg.Any<string>())
				.Returns(call => Path.Combine("clip", call.Arg<string>()));

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			if (encryption is null)
			{
				builder
					.RegisterType<EncryptionService>()
					.As<IEncryptionService>();
			}
			else
			{
				builder.RegisterInstance(encryption);
			}
		});
	}

	/// <summary>
	/// UTF-8 password bytes.
	/// </summary>
	private static byte[] Password(string value) => TextHelper.Utf8Encoding.GetBytes(value);

	/// <summary>
	/// A minimal text entry.
	/// </summary>
	private static ClipboardTextEntry TextEntry(string text) => new()
	{
		Text = text,
		Html = null,
		Rtf = null,
		Hash = [1, 2, 3]
	};
	#endregion
}
