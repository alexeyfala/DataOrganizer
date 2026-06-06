using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using Shared.Interfaces;
using System.IO;
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
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardHistoryStore sut = mock.Create<ClipboardHistoryStore>();

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
	/// Test of <see cref="ClipboardHistoryStore.EraseHistory" />.
	/// </summary>
	[Test]
	public async Task EraseHistory_Removes_Journal_But_Keeps_Key()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardHistoryStore sut = mock.Create<ClipboardHistoryStore>();

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
	/// Test of <see cref="ClipboardHistoryStore.SaveAsync" /> / <see cref="ClipboardHistoryStore.TryUnlockAsync" />.
	/// </summary>
	[Test]
	public async Task Save_Then_Unlock_In_New_Session_Restores_Entries()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = CreateMock(files))
		{
			ClipboardHistoryStore writer = first.Create<ClipboardHistoryStore>();

			await writer.TryUnlockAsync(Password("pw"));

			await writer.SaveAsync([TextEntry("secret")]);
		}

		// Act
		using AutoMock second = CreateMock(files);

		ClipboardHistoryStore reader = second.Create<ClipboardHistoryStore>();

		ClipboardHistoryUnlockResult result = await reader.TryUnlockAsync(Password("pw"));

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
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardHistoryStore sut = mock.Create<ClipboardHistoryStore>();

		// Act
		await sut.SaveAsync([TextEntry("data")]);

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
		InMemoryFileSystem files = new();

		using AutoMock mock = CreateMock(files);

		ClipboardHistoryStore sut = mock.Create<ClipboardHistoryStore>();

		// Act
		ClipboardHistoryUnlockResult result = await sut.TryUnlockAsync(Password("pw"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.Unlocked);

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
	/// Test of <see cref="ClipboardHistoryStore.TryUnlockAsync" />: a corrupt journal yields no entries.
	/// </summary>
	[Test]
	public async Task TryUnlock_With_Corrupt_Journal_Returns_Empty()
	{
		// Arrange
		InMemoryFileSystem files = new();

		using (AutoMock first = CreateMock(files))
		{
			ClipboardHistoryStore writer = first.Create<ClipboardHistoryStore>();

			await writer.TryUnlockAsync(Password("pw"));

			await writer.SaveAsync([TextEntry("data")]);
		}

		files.Files[BinPath] = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

		// Act
		using AutoMock second = CreateMock(files);

		ClipboardHistoryStore reader = second.Create<ClipboardHistoryStore>();

		ClipboardHistoryUnlockResult result = await reader.TryUnlockAsync(Password("pw"));

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
		InMemoryFileSystem files = new();

		using (AutoMock first = CreateMock(files))
		{
			ClipboardHistoryStore writer = first.Create<ClipboardHistoryStore>();

			await writer.TryUnlockAsync(Password("right"));
		}

		// Act
		using AutoMock second = CreateMock(files);

		ClipboardHistoryStore reader = second.Create<ClipboardHistoryStore>();

		ClipboardHistoryUnlockResult result = await reader.TryUnlockAsync(Password("wrong"));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardHistoryUnlockStatus.WrongPassword);

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
	private static AutoMock CreateMock(InMemoryFileSystem files)
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

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();
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
