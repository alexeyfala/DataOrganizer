using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Encryption;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Storage;
using DataOrganizer.Services.Encryption;
using DataOrganizer.Services.Storage;
using DataOrganizer.UnitTests.Factories;
using EditorMessages.Messages;
using Entities.Enums;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.Interfaces;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Repository.Dto;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services.Encryption;

[TestFixture(Description = $@"Tests of ""{nameof(FolderProtection)}"" type")]
internal class FolderProtectionTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderProtection.ChangePasswordAsync" />: a folder that keeps no password is left alone, and no password
	/// is asked for.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Asks_Nothing_For_A_Folder_That_Is_No_Keeper()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(unlocker));

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		await unlocker
			.DidNotReceiveWithAnyArgs()
			.RequestDekAsync(default!, default!);
	}

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

		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			passwordPrompt
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

			builder.RegisterInstance(passwordPrompt);
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

		await passwordPrompt.Received(1).RequestPasswordAsync(
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

		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

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

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(dbAccess);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.ChangePasswordAsync(folder);

		await passwordPrompt
			.DidNotReceiveWithAnyArgs()
			.RequestPasswordAsync(default!);

		await dbAccess
			.DidNotReceive()
			.UpdateEncryptedDekAsync(Arg.Any<Guid>(), Arg.Any<byte[]>());

		folder.EncryptedDek
			.Should()
			.BeSameAs(encryptedDek);
	}

	/// <summary>
	/// <see cref="FolderProtection.ChangePasswordAsync" />: a cancelled new password keeps the key as it is.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Keeps_The_Key_When_The_New_Password_Is_Cancelled()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		IFolderCipher folderCipher = Substitute.For<IFolderCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(10));

			// A cancelled prompt returns an empty password.
			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new PinnedSecret(length: 0));

			builder.RegisterInstance(folderCipher);

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(unlocker);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		await folderCipher
			.DidNotReceiveWithAnyArgs()
			.ChangePasswordAsync(default!, default!, default!);
	}

	/// <summary>
	/// <see cref="FolderProtection.ChangePasswordAsync" />: rewraps the DEK with the new password.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Rewraps_The_Dek()
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity entity = EntityFactory.CreateFolder(encryptedDek: RandomValues.CreateBytes(10));

		FolderDto folder = ItemDtoFactory.CreateFolderDto(id: entity.Id);

		byte[] encryptedDek = RandomValues.CreateBytes(10);

		folder.EncryptedDek = encryptedDek;

		byte[] rewrapped = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			passwordPrompt
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

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			encryption
				.Encrypt(Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(rewrapped);

			builder.RegisterInstance(database.Context);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(unlocker);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();

			builder
				.RegisterType<KeeperDekWriter>()
				.As<IKeeperDekWriter>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([entity]);

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		folder.EncryptedDek
			.Should()
			.BeSameAs(rewrapped);

		FolderEntity[] stored = await dbAccess.GetAllFoldersAsync();

		stored
			.Should()
			.ContainSingle()
			.Which
			.EncryptedDek
			.Should()
			.Equal(rewrapped);
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: a keeper with a child that is not encrypted is left alone, and
	/// no password is asked for.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Asks_Nothing_For_A_Keeper_With_A_Shown_Child()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		FileDto shown = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		shown.Parent = folder;

		folder
			.Children
			.Add(shown);

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(unlocker));

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, [shown]);

		// Assert
		await unlocker
			.DidNotReceiveWithAnyArgs()
			.RequestDekAsync(default!, default!);
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: the notes of a conversion reach the writer.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Delivers_The_Converted_Notes()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

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

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
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

		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

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
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

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

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(contentKeys);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		contentKeys
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
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

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

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
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
		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

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

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await dbBackupService
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
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

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

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
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
	/// <see cref="FolderProtection.DecryptFolderAsync" />: the progress bar shows while the files are decrypted, which keeps
	/// the other commands waiting, and hides after.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Shows_The_Progress_While_It_Decrypts()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		List<bool> progress = [];

		bool[] whileDecrypting = [];

		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<ShowProgressBarMessage>(this, (_, message) => progress.Add(message.IsVisible));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFolderCipher folderCipher = Substitute.For<IFolderCipher>();

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			folderCipher
				.When(x => x.DecryptFolderAsync(
					folder,
					Arg.Any<FileDto[]>(),
					Arg.Any<PinnedBuffer>(),
					Arg.Any<CancellationToken>()))
				.Do(_ => whileDecrypting = [.. progress]);

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(10));

			builder.RegisterInstance(folderCipher);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(unlocker);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.DecryptFolderAsync(folder, []);

		// Assert
		whileDecrypting
			.Should()
			.Equal(true);

		progress
			.Should()
			.Equal(true, false);
	}

	/// <summary>
	/// <see cref="FolderProtection.DecryptFolderAsync" />: a conversion that cannot be saved leaves
	/// neither the decrypted contents nor the decrypted notes readable in memory.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Wipes_The_Plain_Text_When_The_Write_Fails()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

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

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();
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
	/// <see cref="FolderProtection.EncryptFolderAsync(FolderDto, FileDto[], CancellationToken)" />: the password of a new keeper is asked for
	/// with a confirmation, so a typo cannot lock the files away.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Asks_For_A_New_Password()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// An empty result stops the flow right after the prompt, which is all this test looks at.
			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new PinnedSecret(length: 0));

			builder.RegisterInstance(passwordPrompt);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		await passwordPrompt.Received(1).RequestPasswordAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			PasswordPromptMode.Create,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync(FolderDto, FileDto[], CancellationToken)" />: a folder inside a
	/// keeper is left alone, and no password is asked for.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Asks_Nothing_For_A_Folder_Inside_A_Keeper()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		folder.Parent = keeper;

		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(passwordPrompt));

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, [ItemDtoFactory.CreateFileDto()]);

		// Assert
		await passwordPrompt
			.DidNotReceiveWithAnyArgs()
			.RequestPasswordAsync(default!);
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync(FolderDto, FileDto[], CancellationToken)" />: nothing is persisted when a note cannot be encrypted.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Does_Not_Persist_When_A_Note_Cannot_Be_Encrypted()
	{
		// Arrange
		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		IEncryptedContentWriter contentWriter = Substitute.For<IEncryptedContentWriter>();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		files[0].Note = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

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

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		await dbBackupService
			.DidNotReceive()
			.CreateBackupAsync();

		// The write goes through the content writer, so the database is asked nothing at all.
		await contentWriter
			.DidNotReceive()
			.UpdateDatabaseAsync(Arg.Any<UpdateDatabaseParameters>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync(FolderDto, FileDto[], CancellationToken)" />: the copy of the database is erased when the operation ends.
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

			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

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

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(backup);

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
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
	/// <see cref="FolderProtection.EncryptFolderAsync(FolderDto, FileDto[], PinnedSecret, CancellationToken)" />: a folder
	/// inside a keeper is left alone, though the password is given.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Leaves_A_Folder_Inside_A_Keeper_With_The_Password_Given()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		folder.Parent = keeper;

		using PinnedSecret password = SecretFactory.CreateRandomSecret();

		IFolderCipher folderCipher = Substitute.For<IFolderCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(folderCipher));

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, [ItemDtoFactory.CreateFileDto()], password);

		// Assert
		await folderCipher
			.DidNotReceiveWithAnyArgs()
			.EncryptFolderAsync(default!, default!, default!);
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync(FolderDto, FileDto[], CancellationToken)" />: encrypts the folder and persists the updated file properties.
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

			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

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

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
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
	/// <see cref="FolderProtection.EncryptFolderAsync(FolderDto, FileDto[], PinnedSecret, CancellationToken)" />: the
	/// progress bar shows while the files are encrypted, which keeps the other commands waiting, and hides after.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Shows_The_Progress_While_It_Encrypts()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		List<bool> progress = [];

		bool[] whileEncrypting = [];

		using PinnedSecret password = SecretFactory.CreateRandomSecret();

		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<ShowProgressBarMessage>(this, (_, message) => progress.Add(message.IsVisible));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFolderCipher folderCipher = Substitute.For<IFolderCipher>();

			folderCipher
				.When(x => x.EncryptFolderAsync(
					folder,
					Arg.Any<FileDto[]>(),
					password,
					Arg.Any<CancellationToken>()))
				.Do(_ => whileEncrypting = [.. progress]);

			builder.RegisterInstance(folderCipher);

			builder.RegisterInstance(messenger);
		});

		FolderProtection sut = mock.Create<FolderProtection>();

		// Act
		await sut.EncryptFolderAsync(folder, [], password);

		// Assert
		whileEncrypting
			.Should()
			.Equal(true);

		progress
			.Should()
			.Equal(true, false);
	}

	/// <summary>
	/// <see cref="FolderProtection.EncryptFolderAsync(FolderDto, FileDto[], CancellationToken)" />: the plain text loaded for the conversion
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

			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

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

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(contentWriter);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(converter);

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<FolderCipher>()
				.As<IFolderCipher>();
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
