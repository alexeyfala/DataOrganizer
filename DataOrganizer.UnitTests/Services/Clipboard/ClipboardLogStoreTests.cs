using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Clipboard;
using DataOrganizer.Dto.Clipboard.Persistence;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Clipboard;

// Unlocking a store derives a key from a password, which is deliberately expensive. Each test
// owns its file system and its store, so they may run side by side.
[Parallelizable(ParallelScope.Children)]
[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogStore)}"" type")]
internal class ClipboardLogStoreTests
{
	#region Data
	/// <summary>
	/// Directory the clipboard history files live in.
	/// </summary>
	private const string HistoryFolder = "clip";

	/// <summary>
	/// Path of the clipboard history file.
	/// </summary>
	private static readonly string BinPath = Path.Combine(HistoryFolder, "History.bin");

	/// <summary>
	/// Path of the clipboard history key file.
	/// </summary>
	private static readonly string KeyPath = Path.Combine(HistoryFolder, "History.key");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogStore.Dispose" />: the key is given up, so nothing is written afterwards.
	/// </summary>
	[Test]
	public async Task Dispose_Locks_The_Store_And_Stops_Saving()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		await sut.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Act
		sut.Dispose();

		await sut.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);

		// Assert
		sut.IsUnlocked
			.Should()
			.BeFalse();

		files.Files
			.Should()
			.NotContainKey(BinPath);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.EraseAll" />: removes both journal and key files and locks the store.
	/// </summary>
	[Test]
	public async Task EraseAll_Removes_Both_Files_And_Locks()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		await sut.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		await sut.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);

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
	public async Task EraseHistory_Removes_Log_But_Keeps_Key()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		await sut.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		await sut.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);

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
	public async Task LoadEntriesAsync_With_Unsupported_Version_Returns_Empty()
	{
		// Arrange
		InMemoryFileSystem files = new();

		byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(new PersistedClipboardLog
		{
			Version = PersistedClipboardLog.CurrentVersion + 1
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			sessionKeyStore
				.Decrypt(default, default, default!)
				.ReturnsForAnyArgs(plaintext);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder.RegisterInstance(sessionKeyStore);
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		// The key store is faked, so the stored bytes are never read as a real ciphertext.
		files.Files[BinPath] = [1, 2, 3];

		// Act
		IReadOnlyList<ClipboardLogEntryBase> result = await sut.LoadEntriesAsync(default);

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.SaveAsync" />: the journal is put in place in one step, so a save
	/// that does not finish leaves the previous one readable.
	/// </summary>
	[Test]
	public async Task SaveAsync_Replaces_The_Log_Atomically()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		await sut.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Act
		await sut.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);

		// Assert
		files.AtomicWrites
			.Should()
			.Contain(BinPath);

		files.Files
			.Should()
			.ContainKey(BinPath);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.SaveAsync" /> / <see cref="ClipboardLogStore.TryUnlockAsync" />: a saved entry is restored after unlocking in a new session.
	/// </summary>
	[Test]
	public async Task SaveAsync_Then_Unlock_In_New_Session_Restores_Entries()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		}))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

			await writer.SaveAsync([ClipboardEntryFactory.CreateTextEntry("secret")]);
		}

		// Act
		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardLogUnlockResult result = await reader.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Unlocked);

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
	public async Task SaveAsync_Twice_Overwrites_Previous_Log()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		}))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

			await writer.SaveAsync([ClipboardEntryFactory.CreateTextEntry("old")]);

			await writer.SaveAsync([ClipboardEntryFactory.CreateTextEntry("new")]);
		}

		// Act
		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardLogUnlockResult result = await reader.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

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
	public async Task SaveAsync_When_Encryption_Fails_Writes_Nothing()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			// The key is taken (so the store unlocks)...
			sessionKeyStore
				.Unlock(default, default!)
				.ReturnsForAnyArgs(true);

			sessionKeyStore
				.IsUnlocked(default)
				.ReturnsForAnyArgs(true);

			// ...but encrypting the journal fails.
			sessionKeyStore
				.Encrypt(default, default, default!)
				.ThrowsForAnyArgs(new CryptographicException());

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder.RegisterInstance(sessionKeyStore);
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		await sut.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Act
		await sut.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);

		// Assert
		files.Files
			.Should()
			.NotContainKey(BinPath);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.SaveAsync" />: writes nothing while locked.
	/// </summary>
	[Test]
	public async Task SaveAsync_Without_Unlock_Writes_Nothing()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		// Act
		await sut.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);

		// Assert
		files.Files
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a new key is created when none exists,
	/// and is written in one step.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_Creates_Key_When_None_Exists()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		// Act
		ClipboardLogUnlockResult result = await sut.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Unlocked);

		result.Entries
			.Should()
			.BeEmpty();

		sut.IsUnlocked
			.Should()
			.BeTrue();

		sut.KeyFileExists
			.Should()
			.BeTrue();

		files.AtomicWrites
			.Should()
			.Contain(KeyPath);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a key written at a new derivation cost replaces
	/// the old one in one step, so an interrupted rewrap keeps the journal openable.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_Replaces_A_Rewrapped_Key_Atomically()
	{
		// Arrange
		InMemoryFileSystem files = new();

		// The store only asks whether a key file is there, and its bytes go to the substituted
		// encryption below, so they never have to be a real key.
		files.Files[KeyPath] = [1, 2, 3];

		byte[] rewrapped = [9, 8, 7];

		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(new PinnedBuffer(32));

			encryption
				.RewrapIfOutdated(
					Arg.Any<byte[]>(),
					Arg.Any<PinnedBuffer>(),
					Arg.Any<PinnedBuffer>(),
					Arg.Any<ContentIdentity>())
				.Returns(rewrapped);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder.RegisterInstance(encryption);

			// The key store keeps a real encryption service even though the store gets a substituted one.
			builder.RegisterInstance<ISessionKeyStore>(new SessionKeyStore(new EncryptionService()));
		});

		ClipboardLogStore sut = second.Create<ClipboardLogStore>();

		// Act
		ClipboardLogUnlockResult result = await sut.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Unlocked);

		files.AtomicWrites
			.Should()
			.Contain(KeyPath);

		files.Files[KeyPath]
			.Should()
			.BeEquivalentTo(rewrapped);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: an existing key rejects the password on its own,
	/// so a cryptographic failure behind it is the data and no further password is asked for.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_When_An_Existing_Key_Cannot_Be_Read_Returns_Damaged()
	{
		// Arrange
		InMemoryFileSystem files = new();

		// The bytes of the key reach the substituted encryption below, which answers for them,
		// so they never have to be a real key.
		files.Files[KeyPath] = [1, 2, 3];

		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())!
				.Throws(new AuthenticationTagMismatchException());

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder.RegisterInstance(encryption);

			// The key store keeps a real encryption service even though the store gets a substituted one.
			builder.RegisterInstance<ISessionKeyStore>(new SessionKeyStore(new EncryptionService()));
		});

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		// Act
		ClipboardLogUnlockResult result = await reader.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Damaged);

		reader.IsUnlocked
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: rejected credentials yield WrongPassword.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_When_Key_Unwrap_Is_Rejected_Returns_WrongPassword()
	{
		// Arrange
		InMemoryFileSystem files = new();

		// The bytes of the key reach the substituted encryption below, which answers for them,
		// so they never have to be a real key.
		files.Files[KeyPath] = [1, 2, 3];

		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())!
				.Throws(new InvalidCredentialException());

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder.RegisterInstance(encryption);

			// The key store keeps a real encryption service even though the store gets a substituted one.
			builder.RegisterInstance<ISessionKeyStore>(new SessionKeyStore(new EncryptionService()));
		});

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		// Act
		ClipboardLogUnlockResult result = await reader.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.WrongPassword);

		reader.IsUnlocked
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a failure to wrap a new key yields Failed.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_When_Key_Wrap_Fails_Returns_Failed()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			encryption
				.CreateRandomDek()
				.Returns(new PinnedBuffer(32));

			encryption
				.Encrypt(Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Throws(new CryptographicException());

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder.RegisterInstance(encryption);

			// The key store keeps a real encryption service even though the store gets a substituted one.
			builder.RegisterInstance<ISessionKeyStore>(new SessionKeyStore(new EncryptionService()));
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		// Act
		ClipboardLogUnlockResult result = await sut.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Failed);

		sut.IsUnlocked
			.Should()
			.BeFalse();

		files.Files
			.Should()
			.NotContainKey(KeyPath);
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a journal that fails authentication leaves the store unlocked and empty.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_When_Log_Is_Rejected_Returns_Empty()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		}))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

			await writer.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);
		}

		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			// The key file yields a key of the right size but the wrong value, so the journal is unreadable.
			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(new PinnedBuffer(32));

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder.RegisterInstance(encryption);

			// The key store keeps a real encryption service even though the store gets a substituted one.
			builder.RegisterInstance<ISessionKeyStore>(new SessionKeyStore(new EncryptionService()));
		});

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		// Act
		ClipboardLogUnlockResult result = await reader.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Unlocked);

		result.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a corrupt journal yields no entries.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_With_Corrupt_Log_Returns_Empty()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		}))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

			await writer.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);
		}

		files.Files[BinPath] = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

		// Act
		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardLogUnlockResult result = await reader.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Unlocked);

		result.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a journal truncated to nothing is treated as
	/// damaged data, so the session still opens.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_With_Empty_Log_Returns_Empty()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		}))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

			await writer.SaveAsync([ClipboardEntryFactory.CreateTextEntry("data")]);
		}

		files.Files[BinPath] = [];

		// Act
		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardLogUnlockResult result = await reader.TryUnlockAsync(SecretFactory.CreatePassword("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Unlocked);

		result.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a wrong password is rejected.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_With_Wrong_Password_Returns_WrongPassword()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		}))
		{
			ClipboardLogStore writer = first.Create<ClipboardLogStore>();

			await writer.TryUnlockAsync(SecretFactory.CreatePassword("right"));
		}

		// Act
		using AutoMock second = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore reader = second.Create<ClipboardLogStore>();

		ClipboardLogUnlockResult result = await reader.TryUnlockAsync(SecretFactory.CreatePassword("wrong"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.WrongPassword);

		reader.IsUnlocked
			.Should()
			.BeFalse();
	}
	#endregion
}
