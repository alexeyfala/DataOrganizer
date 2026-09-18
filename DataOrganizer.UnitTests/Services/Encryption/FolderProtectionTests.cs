using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Encryption;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Factories;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Shared.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;

namespace DataOrganizer.UnitTests.Services.Encryption;

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
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		IDialogService dialogService = Substitute.For<IDialogService>();

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(dialogService);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		await unlocker.Received(1).RequestDekAsync(
			folder,
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<string>());

		await dialogService.Received(1).RequestPasswordAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
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
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		byte[] encryptedDek = RandomValues.CreateBytes(10);

		folder.EncryptedDek = encryptedDek;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// A rejected password leaves the unlocker with nothing to hand over.
			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns((PinnedBuffer?)null);

			builder.RegisterInstance(unlocker);

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
			.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderEntity>>[]>());

		folder.EncryptedDek
			.Should()
			.BeSameAs(encryptedDek);
	}

	/// <summary>
	/// <see cref="FolderProtection.ChangePasswordAsync" />: rewraps the DEK with the new password.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Rewraps_The_Dek()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		byte[] encryptedDek = RandomValues.CreateBytes(10);

		folder.EncryptedDek = encryptedDek;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(10));

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Encrypt(Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(RandomValues.CreateBytes(10));

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderEntity>>[]>())
				.Returns(true);

			builder.RegisterInstance(unlocker);

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
	/// <see cref="FolderProtection.DecryptFolderAsync" />: the notes of a conversion reach the writer.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Delivers_The_Converted_Notes()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(1)];

		NoteUpdate[] notes =
		[
			new(folder.Id, EntityKind.Folder, RandomValues.CreateBytes(10)),
			new(files[0].Id, EntityKind.File, RandomValues.CreateBytes(10))
		];

		IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(UpdateDatabaseOutcome.Saved);

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns(new FolderConversion
				{
					Converted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)],
					Loaded = [],
					Notes = notes
				});

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await contentWriter.Received(1).UpdateDatabaseAsync(
			Arg.Is<UpdateDatabaseParameters>(x => x.Notes == notes),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: a wrong password never pulls the contents of the files into memory.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Does_Not_Load_Contents_When_The_Password_Is_Wrong()
	{
		// Arrange
		IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// A rejected password leaves the unlocker with nothing to hand over.
			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns((PinnedBuffer?)null);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await converter
			.DidNotReceiveWithAnyArgs()
			.ConvertAsync(default!);
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: a converted folder keeps no password,
	/// so its session key goes with it.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Drops_The_Key_Of_A_Converted_Folder([Values] bool isWriteSaved)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(isWriteSaved ? UpdateDatabaseOutcome.Saved : UpdateDatabaseOutcome.SaveFailed);

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns(new FolderConversion
				{
					Converted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)],
					Loaded = [],
					Notes = []
				});

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		contentVisibility
			.Received(isWriteSaved ? 1 : 0)
			.DiscardKeys(folder);
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: a done conversion keeps its notes, from then
	/// on they belong to the objects.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Keeps_The_Notes_Of_A_Done_Conversion()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder.Note = RandomValues.CreateBytes(10);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(1)];

		byte[] decryptedNote = [5, 6, 7, 8];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(UpdateDatabaseOutcome.Saved);

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns(new FolderConversion
				{
					Converted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)],
					Loaded = [],
					Notes = [new(folder.Id, EntityKind.Folder, decryptedNote)]
				});

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		decryptedNote
			.Should()
			.Equal([5, 6, 7, 8]);
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: a conversion that ends without a result
	/// stops the operation before anything is written.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Refuses_A_Failed_Conversion()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

		IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(UpdateDatabaseOutcome.Saved);

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns((FolderConversion?)null);

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.CreateBackupAsync();

		await contentWriter
			.DidNotReceiveWithAnyArgs()
			.UpdateDatabaseAsync(default!);
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: decrypts the folder and persists the updated file properties.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Saves_The_Decrypted_Contents()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

		IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(UpdateDatabaseOutcome.Saved);

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(dek);

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns(new FolderConversion
				{
					Converted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)],
					Loaded = [],
					Notes = []
				});

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		// The direction and the key are what decide whether the files stay readable.
		await converter.Received(1).ConvertAsync(
			Arg.Is<FolderConversionParameters>(x =>
				!x.Encrypt
				&& x.Dek == dek
				&& x.Folder == folder
				&& x.Files == files),
			Arg.Any<CancellationToken>());

		await contentWriter.Received(1).UpdateDatabaseAsync(
			Arg.Is<UpdateDatabaseParameters>(x =>
				x.EncryptedDek == null
				&& x.NewStatus == EncryptionStatus.None
				&& x.Contents.Length == files.Length),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: a conversion that cannot be saved leaves
	/// neither the decrypted contents nor the decrypted notes readable in memory.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Wipes_The_Plain_Text_When_The_Write_Fails()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder.Note = RandomValues.CreateBytes(10);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(1)];

		byte[] decryptedContents = [1, 2, 3, 4];

		byte[] decryptedNote = [5, 6, 7, 8];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(UpdateDatabaseOutcome.SaveFailed);

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns(new FolderConversion
				{
					Converted =
					[
						new ValidatedContents
						{
							Contents = decryptedContents,
							Id = files[0].Id,
							IsValid = true
						}
					],
					Loaded = [],
					Notes = [new(folder.Id, EntityKind.Folder, decryptedNote)]
				});

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		decryptedContents.Should().AllSatisfy(x => x
			.Should()
			.Be(0));

		decryptedNote.Should().AllSatisfy(x => x
			.Should()
			.Be(0));
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: the password of a new keeper is asked for
	/// with a confirmation, so a typo cannot lock the files away.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Asks_For_A_New_Password()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

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

		IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		files[0].Note = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Encrypt(Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns([]);

			IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

			// A note that cannot be encrypted comes out of the conversion as a cryptographic failure.
			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.ThrowsAsync(new CryptographicException());

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(converter);

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
			.CreateBackupAsync();

		// The write goes through the content writer, so the database is asked nothing at all.
		await contentWriter
			.DidNotReceive()
			.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: the copy of the database is erased when the operation ends.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Erases_The_Database_Backup([Values] bool isUpdateFailing)
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		DatabaseBackup backup = DatabaseFactory.CreateDatabaseBackup(fileSystem);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(1)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(UpdateDatabaseOutcome.Saved);

			// The failing branch is the one that used to leave the copy behind.
			if (isUpdateFailing)
			{
				contentWriter
					.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
					.Returns(UpdateDatabaseOutcome.ExceptionThrown);
			}

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Encrypt(Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns([]);

			IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns(new FolderConversion
				{
					Converted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)],
					Loaded = [],
					Notes = []
				});

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(backup);

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		fileSystem
			.Received(1)
			.EraseAndDeleteFile(backup.FilePath);
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: encrypts the folder and persists the updated file properties.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Saves_The_Encrypted_Contents()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

		IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(UpdateDatabaseOutcome.Saved);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.CreateRandomDek()
				.Returns(dek);

			encryption
				.Encrypt(Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns([]);

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns(new FolderConversion
				{
					Converted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)],
					Loaded = [],
					Notes = []
				});

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		// The direction and the key are what decide whether the files stay readable.
		await converter.Received(1).ConvertAsync(
			Arg.Is<FolderConversionParameters>(x =>
				x.Encrypt
				&& x.Dek == dek
				&& x.Folder == folder
				&& x.Files == files),
			Arg.Any<CancellationToken>());

		await contentWriter.Received(1).UpdateDatabaseAsync(
			Arg.Is<UpdateDatabaseParameters>(x =>
				x.NewStatus == EncryptionStatus.Encrypted
				&& x.Contents.Length == files.Length),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync" />: the plain text loaded for the conversion
	/// is wiped.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Wipes_The_Loaded_Contents()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(1)];

		ValidatedContents[] loaded =
		[
			new()
			{
				Contents = [1, 2, 3, 4],
				Id = files[0].Id,
				IsValid = true
			}
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

			contentWriter
				.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>())
				.Returns(UpdateDatabaseOutcome.Saved);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Encrypt(Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns([]);

			IFolderContentsConverter converter = Substitute.For<IFolderContentsConverter>();

			converter
				.ConvertAsync(Arg.Any<FolderConversionParameters>(), Arg.Any<CancellationToken>())
				.Returns(new FolderConversion
				{
					Converted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)],
					Loaded = loaded,
					Notes = []
				});

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		loaded[0].Contents.Should().AllSatisfy(x => x
			.Should()
			.Be(0));
	}
	#endregion
}
