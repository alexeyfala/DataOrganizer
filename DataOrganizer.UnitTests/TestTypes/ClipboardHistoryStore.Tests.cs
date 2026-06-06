using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.UnitTests.Helpers;
using Moq;
using Serilog;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardHistoryStore)}"" type")]
internal class ClipboardHistoryStoreTests
{
	#region Data
	private static readonly string BinPath = Path.Combine("clip", "History.bin");

	private static readonly string KeyPath = Path.Combine("clip", "History.key");
	#endregion

	#region Methods
	/// <summary>
	/// Test of <see cref="ClipboardHistoryStore.EraseAll" />.
	/// </summary>
	[Test]
	public async Task EraseAll_Removes_Both_Files_And_Locks()
	{
		// Arrange
		(ClipboardHistoryStore store, InMemoryFileSystem files) = CreateStore();

		await store.TryUnlockAsync(Password("pw"));

		await store.SaveAsync([TextEntry("data")]);

		// Act
		store.EraseAll();

		// Assert
		store.IsUnlocked
			.Should()
			.BeFalse();

		store.KeyFileExists
			.Should()
			.BeFalse();

		files.Files
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryStore.EraseHistory" />.
	/// </summary>
	[Test]
	public async Task EraseHistory_Removes_Journal_But_Keeps_Key()
	{
		// Arrange
		(ClipboardHistoryStore store, InMemoryFileSystem files) = CreateStore();

		await store.TryUnlockAsync(Password("pw"));

		await store.SaveAsync([TextEntry("data")]);

		// Act
		store.EraseHistory();

		// Assert
		store.IsUnlocked
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
	/// Test of <see cref="ClipboardHistoryStore.SaveAsync" /> / <see cref="ClipboardHistoryStore.TryUnlockAsync" />.
	/// </summary>
	[Test]
	public async Task Save_Then_Unlock_In_New_Session_Restores_Entries()
	{
		// Arrange
		(ClipboardHistoryStore first, InMemoryFileSystem files) = CreateStore();

		await first.TryUnlockAsync(Password("pw"));

		await first.SaveAsync([TextEntry("secret")]);

		// Act
		(ClipboardHistoryStore second, _) = CreateStore(files);

		ClipboardHistoryUnlockResult result = await second.TryUnlockAsync(Password("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.Unlocked);

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
	/// Test of <see cref="ClipboardHistoryStore.SaveAsync" />: writes nothing while locked.
	/// </summary>
	[Test]
	public async Task Save_Without_Unlock_Writes_Nothing()
	{
		// Arrange
		(ClipboardHistoryStore store, InMemoryFileSystem files) = CreateStore();

		// Act
		await store.SaveAsync([TextEntry("data")]);

		// Assert
		files.Files
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryStore.TryUnlockAsync" />: a new key is created when none exists.
	/// </summary>
	[Test]
	public async Task TryUnlock_Creates_Key_When_None_Exists()
	{
		// Arrange
		(ClipboardHistoryStore store, _) = CreateStore();

		// Act
		ClipboardHistoryUnlockResult result = await store.TryUnlockAsync(Password("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.Unlocked);

		result.Entries
			.Should()
			.BeEmpty();

		store.IsUnlocked
			.Should()
			.BeTrue();

		store.KeyFileExists
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryStore.TryUnlockAsync" />: a corrupt journal yields no entries.
	/// </summary>
	[Test]
	public async Task TryUnlock_With_Corrupt_Journal_Returns_Empty()
	{
		// Arrange
		(ClipboardHistoryStore first, InMemoryFileSystem files) = CreateStore();

		await first.TryUnlockAsync(Password("pw"));

		await first.SaveAsync([TextEntry("data")]);

		files.Files[BinPath] = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

		// Act
		(ClipboardHistoryStore second, _) = CreateStore(files);

		ClipboardHistoryUnlockResult result = await second.TryUnlockAsync(Password("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.Unlocked);

		result.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryStore.TryUnlockAsync" />: a wrong password is rejected.
	/// </summary>
	[Test]
	public async Task TryUnlock_With_Wrong_Password_Returns_WrongPassword()
	{
		// Arrange
		(ClipboardHistoryStore first, InMemoryFileSystem files) = CreateStore();

		await first.TryUnlockAsync(Password("right"));

		// Act
		(ClipboardHistoryStore second, _) = CreateStore(files);

		ClipboardHistoryUnlockResult result = await second.TryUnlockAsync(Password("wrong"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.WrongPassword);

		second.IsUnlocked
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a store backed by an in-memory file system and a real encryption service.
	/// </summary>
	private static (ClipboardHistoryStore Store, InMemoryFileSystem Files) CreateStore(InMemoryFileSystem? files = null)
	{
		files ??= new InMemoryFileSystem();

		Mock<IAppEnvironment> appEnvironment = new();

		appEnvironment
			.Setup(x => x.GetClipboardHistoryFilePath(It.IsAny<string>()))
			.Returns<string>(name => Path.Combine("clip", name));

		EncryptionService encryption = new(Mock.Of<ILogger>());

		ClipboardHistoryStore store = new(
			appEnvironment.Object,
			encryption,
			files,
			Mock.Of<ILogger>());

		return (store, files);
	}

	/// <summary>
	/// UTF-8 password bytes.
	/// </summary>
	private static byte[] Password(string value) => Encoding.UTF8.GetBytes(value);

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
