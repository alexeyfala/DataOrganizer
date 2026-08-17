using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Helpers;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Repository.DTO;
using Repository.Interfaces;
using Repository.Services;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(FolderProtection)}"" type")]
internal class FolderProtectionTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderProtection.ChangePasswordAsync" />: the old password goes to the unlocker,
	/// while the new one is asked for with a confirmation.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Confirms_Only_The_New_Password()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		byte[] encryptedDek = TestUtils.CreateRandomBytes(10);

		folder.EncryptedDek = encryptedDek;

		IDialogService dialogService = Substitute.For<IDialogService>();

		IKeeperUnlocker unlocker = null!;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			unlocker = RegisterUnlocker(builder, TestUtils.CreateRandomBytes(32));

			builder.RegisterInstance(dialogService);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		await unlocker.Received(1).RequestDekAsync(
			folder.Id,
			encryptedDek,
			Arg.Any<string>(),
			Strings.OldPassword,
			Arg.Any<CancellationToken>(),
			Arg.Any<string>());

		await dialogService.Received(1).RequestPasswordAsync(
			Arg.Any<string>(),
			Strings.NewPassword,
			Arg.Any<string>(),
			PasswordPromptMode.Create,
			Arg.Any<CancellationToken>());
	}


	/// <summary>
	/// <see cref="FolderProtection.ChangePasswordAsync" />: a wrong old password is reported before a new one is asked for.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Does_Not_Ask_For_A_New_Password_When_The_Old_One_Is_Wrong()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		byte[] encryptedDek = TestUtils.CreateRandomBytes(10);

		folder.EncryptedDek = encryptedDek;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// A rejected password leaves the unlocker with nothing to hand over.
			RegisterUnlocker(builder, dek: null);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.ChangePasswordAsync(folder);

		await dialogService
			.DidNotReceiveWithAnyArgs()
			.RequestPasswordAsync(default!);

		await dbAccess
			.DidNotReceive()
			.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>());

		folder.EncryptedDek
			.Should()
			.BeSameAs(encryptedDek);
	}


	/// <summary>
	/// <see cref="FolderProtection.ChangePasswordAsync" />: rewraps the DEK with the new password.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		byte[] encryptedDek = TestUtils.CreateRandomBytes(10);

		folder.EncryptedDek = encryptedDek;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			RegisterUnlocker(builder, TestUtils.CreateRandomBytes(10));

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(TestUtils.CreateRandomBytes(10));

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		folder.EncryptedDek
			.Should()
			.NotBeEquivalentTo(encryptedDek);
	}


	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: the notes of the whole subtree are decrypted and persisted.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Decrypts_Notes()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.Note = TestUtils.CreateRandomBytes(10);

		FolderModelDto subfolder = TestUtils.CreateFolderDto();

		subfolder.Note = TestUtils.CreateRandomBytes(10);

		folder
			.Children
			.Add(subfolder);

		FileModelDto file = TestUtils.CreateFileDto();

		file.Note = TestUtils.CreateRandomBytes(10);

		FileModelDto[] files = [file];

		byte[] decryptedNote = TestUtils.CreateRandomBytes(10);

		IEncryptedContentWriter contentWriter = null!;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentWriter = RegisterContentWriter(builder);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			RegisterUnlocker(builder, TestUtils.CreateRandomBytes(32));

			encryption
				.DecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<ContentIdentity>())
				.Returns(decryptedNote);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>())
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await contentWriter.Received(1).UpdateDatabaseAsync(
			Arg.Is<UpdateDatabaseParameters>(x =>
				x.Notes.Length == 3
				&& x.Notes.Any(note => note.Id == folder.Id)
				&& x.Notes.Any(note => note.Id == subfolder.Id)
				&& x.Notes.Any(note => note.Id == file.Id)
				&& x.Notes.All(note => note.Note == decryptedNote)),
			Arg.Any<CancellationToken>());
	}


	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: a wrong password never pulls the contents of the files into memory.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Does_Not_Load_Contents_When_The_Password_Is_Wrong()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			// A rejected password leaves the unlocker with nothing to hand over.
			RegisterUnlocker(builder, dek: null);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		dbAccess
			.DidNotReceive()
			.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>());
	}


	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: decrypts the folder and persists the updated file properties.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Does_Work()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		IEncryptedContentWriter contentWriter = null!;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentWriter = RegisterContentWriter(builder);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			RegisterUnlocker(builder, TestUtils.CreateRandomBytes(32));

			encryption
				.DecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await contentWriter.Received(1).UpdateDatabaseAsync(
			Arg.Is<UpdateDatabaseParameters>(x =>
				x.EncryptedDek == null
				&& x.NewStatus == EncryptionStatus.None
				&& x.Contents.Length == files.Length),
			Arg.Any<CancellationToken>());
	}


	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: the password of a new keeper is asked for
	/// with a confirmation, so a typo cannot lock the files away.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Asks_For_A_New_Password()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files = [TestUtils.CreateFileDto()];

		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// An empty result stops the flow right after the prompt, which is all this test looks at.
			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new PinnedSecret(length: 0));

			builder.RegisterInstance(dialogService);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		await dialogService.Received(1).RequestPasswordAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			PasswordPromptMode.Create,
			Arg.Any<CancellationToken>());
	}


	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: nothing is persisted when a note cannot be encrypted.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Does_Not_Persist_When_A_Note_Cannot_Be_Encrypted()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		files[0].Note = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns([]);

			encryption
				.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<ContentIdentity>())!
				.Throws(new CryptographicException());

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		await dbAccess
			.DidNotReceive()
			.BackupDatabaseAsync();

		await dbAccess
			.DidNotReceive()
			.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>());
	}


	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: encrypts the folder and persists the updated file properties.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Does_Work()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		IEncryptedContentWriter contentWriter = null!;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentWriter = RegisterContentWriter(builder);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns([]);

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		await contentWriter.Received(1).UpdateDatabaseAsync(
			Arg.Is<UpdateDatabaseParameters>(x =>
				x.NewStatus == EncryptionStatus.Encrypted
				&& x.Contents.Length == files.Length),
			Arg.Any<CancellationToken>());
	}


	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: the note of an object is encrypted with the DEK of the folder.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Encrypts_Notes()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto file = TestUtils.CreateFileDto();

		file.Note = TestUtils.CreateRandomBytes(10);

		FileModelDto[] files = [file];

		byte[] encryptedNote = TestUtils.CreateRandomBytes(10);

		IEncryptedContentWriter contentWriter = null!;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentWriter = RegisterContentWriter(builder);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns([]);

			encryption
				.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<ContentIdentity>())
				.Returns(encryptedNote);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		await contentWriter.Received(1).UpdateDatabaseAsync(
			Arg.Is<UpdateDatabaseParameters>(x =>
				x.Notes.Length == 1
				&& x.Notes[0].Id == file.Id
				&& x.Notes[0].Note == encryptedNote),
			Arg.Any<CancellationToken>());
	}


	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: the copy of the database is erased when the operation ends.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Erases_The_Database_Backup([Values] bool isUpdateFailing)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		DatabaseBackup backup = TestUtils.CreateDatabaseBackup(fileSystem);

		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(1)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptedContentWriter contentWriter = RegisterContentWriter(builder);

			// The failing branch is the one that used to leave the copy behind.
			if (isUpdateFailing)
			{
				contentWriter
					.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
					.Returns(UpdateDatabaseResult.ExceptionThrown);
			}

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns([]);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabaseAsync()
				.Returns(backup);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		fileSystem
			.Received()
			.EraseAndDeleteFile(backup.FilePath);
	}

	#endregion

	#region Helpers
	/// <summary>
	/// Registers a content writer that reports a successful write.
	/// </summary>
	private static IEncryptedContentWriter RegisterContentWriter(ContainerBuilder builder)
	{
		IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

		contentWriter
			.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
			.Returns(UpdateDatabaseResult.Done);

		builder.RegisterInstance(contentWriter);

		return contentWriter;
	}

	/// <summary>
	/// Registers an unlocker that hands the key over without a prompt; <c>null</c> stands for a refusal.
	/// </summary>
	private static IKeeperUnlocker RegisterUnlocker(ContainerBuilder builder, byte[]? dek)
	{
		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		unlocker.RequestDekAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<string>())
			.Returns(dek);

		builder.RegisterInstance(unlocker);

		return unlocker;
	}
	#endregion
}
