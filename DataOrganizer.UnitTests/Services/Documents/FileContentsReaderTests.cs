using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(FileContentsReader)}"" type")]
internal class FileContentsReaderTests
{
	#region Data
	/// <summary>
	/// Header of the password prompt.
	/// </summary>
	private const string Header = "Copy content";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FileContentsReader.ReadAsync" />: the locked keeper of an encrypted file is asked for its password under
	/// the header, and its key opens the contents.
	/// </summary>
	[Test]
	public async Task ReadAsync_Asks_For_The_Password_When_The_File_Is_Encrypted()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] opened = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] stored = RandomValues.CreateBytes(16);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = stored,
					IsValid = true
				});

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					folder,
					Header,
					Arg.Any<string?>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(10));

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.DecryptWithDek(stored, Arg.Any<PinnedBuffer>(), ContentIdentity.Contents)
				.Returns(opened);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(encryption);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(new FileContentsReading
			{
				Contents = opened,
				Status = FileContentsStatus.Opened
			});
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadAsync" />: a file belonging to no password keeper cannot be decrypted, so no
	/// password is asked for.
	/// </summary>
	[Test]
	public async Task ReadAsync_Does_Not_Ask_For_A_Password_Without_A_Keeper()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(10),
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(unlocker);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Closed);

		await unlocker
			.DidNotReceiveWithAnyArgs()
			.RequestDekAsync(default!, default!);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadAsync" />: empty contents are stored unencrypted, so they come back untouched and no
	/// password is asked for.
	/// </summary>
	[Test]
	public async Task ReadAsync_Hands_Empty_Contents_Back()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = [],
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(unlocker);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Opened);

		result.Contents
			.Should()
			.BeEmpty();

		await unlocker
			.DidNotReceiveWithAnyArgs()
			.RequestDekAsync(default!, default!);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadAsync" />: the contents of a file that is not encrypted come back as they are
	/// stored.
	/// </summary>
	[Test]
	public async Task ReadAsync_Hands_The_Contents_Of_A_Plain_File_Back()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.None);

		byte[] stored = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = stored,
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(new FileContentsReading
			{
				Contents = stored,
				Status = FileContentsStatus.Opened
			});
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadAsync" />: the contents of an encrypted file stay closed when the password is not
	/// given.
	/// </summary>
	[Test]
	public async Task ReadAsync_Keeps_The_Contents_Closed_When_No_Password_Is_Given()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(16),
					IsValid = true
				});

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			// The prompt is cancelled.
			unlocker
				.RequestDekAsync(
					folder,
					Header,
					Arg.Any<string?>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns((PinnedBuffer?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(encryption);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Closed);

		encryption
			.DidNotReceiveWithAnyArgs()
			.DecryptWithDek(default!, default!, default);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadAsync" />: the contents of an encrypted file stay closed instead of throwing when the
	/// key of the password does not open them.
	/// </summary>
	[Test]
	public async Task ReadAsync_Keeps_The_Contents_Closed_When_The_Password_Does_Not_Open_Them()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(16),
					IsValid = true
				});

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					folder,
					Header,
					Arg.Any<string?>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(10));

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), ContentIdentity.Contents)
				.Throws(new AuthenticationTagMismatchException());

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(encryption);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Closed);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadAsync" />: the contents under an unlocked keeper are opened with the key of the
	/// session, and no password is asked for.
	/// </summary>
	[Test]
	public async Task ReadAsync_Opens_The_Contents_Under_An_Unlocked_Keeper_Without_A_Password()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		byte[] opened = RandomValues.CreateBytes(10);

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] stored = RandomValues.CreateBytes(16);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = stored,
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.Decrypt(file, stored)
				.Returns(opened);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(unlocker);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(new FileContentsReading
			{
				Contents = opened,
				Status = FileContentsStatus.Opened
			});

		await unlocker
			.DidNotReceiveWithAnyArgs()
			.RequestDekAsync(default!, default!);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadAsync" />: contents that fail their check are never opened, so no password is asked
	/// for.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reports_Damaged_Contents_Without_Opening_Them()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(16),
					IsValid = false
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(unlocker);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Damaged);

		await unlocker
			.DidNotReceiveWithAnyArgs()
			.RequestDekAsync(default!, default!);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(FileDto, CancellationToken)" />: the contents under an unlocked keeper
	/// are opened with the key of the session.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Decrypts_The_Contents_Under_An_Unlocked_Keeper()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		byte[] opened = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] stored = RandomValues.CreateBytes(16);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = stored,
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.Decrypt(file, stored)
				.Returns(opened);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result
			.Should()
			.Be(new FileContentsReading
			{
				Contents = opened,
				Status = FileContentsStatus.Opened
			});
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(Guid, Guid?, CancellationToken)" />: empty contents are stored
	/// unencrypted, so they come back untouched although a keeper is given.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Hands_Empty_Contents_Back_Although_A_Keeper_Is_Given()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(fileId, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = [],
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(fileId, Guid.NewGuid());

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Opened);

		result.Contents
			.Should()
			.BeEmpty();

		contentCipher
			.DidNotReceive()
			.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(Guid, Guid?, CancellationToken)" />: the contents come back as they
	/// are stored when no keeper is given.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Hands_The_Contents_Back_When_No_Keeper_Is_Given()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		byte[] stored = RandomValues.CreateBytes(10);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(fileId, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = stored,
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(fileId, null);

		// Assert
		result
			.Should()
			.Be(new FileContentsReading
			{
				Contents = stored,
				Status = FileContentsStatus.Opened
			});

		contentCipher
			.DidNotReceive()
			.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(FileDto, CancellationToken)" />: the contents of a file that is not
	/// encrypted come back as they are stored.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Hands_The_Contents_Of_A_Plain_File_Back()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		byte[] stored = RandomValues.CreateBytes(10);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = stored,
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result
			.Should()
			.Be(new FileContentsReading
			{
				Contents = stored,
				Status = FileContentsStatus.Opened
			});

		contentCipher
			.DidNotReceive()
			.Decrypt(Arg.Any<FileDto>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(FileDto, CancellationToken)" />: the contents under a locked keeper
	/// stay closed, and no password is asked for.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Keeps_A_Locked_File_Closed()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(16),
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(unlocker);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Closed);

		await unlocker
			.DidNotReceiveWithAnyArgs()
			.RequestDekAsync(default!, default!);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(Guid, Guid?, CancellationToken)" />: the contents stay closed when
	/// the key of the keeper given refuses them.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Keeps_The_Contents_Closed_When_The_Key_Of_The_Keeper_Given_Refuses_Them()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(fileId, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(16),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The key does not open the contents.
			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), ContentIdentity.Contents, Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(fileId, Guid.NewGuid());

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Closed);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(FileDto, CancellationToken)" />: contents the key of the session cannot
	/// open stay closed instead of throwing.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(SessionCipherFailures))]
	public async Task ReadUnlockedAsync_Keeps_The_File_Closed_When_The_Decryption_Fails(Exception failure)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(16),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.Decrypt(file, Arg.Any<byte[]>())
				.Throws(failure);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Closed);
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(Guid, Guid?, CancellationToken)" />: the contents are opened with the
	/// key of the keeper given.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Opens_The_Contents_With_The_Key_Of_The_Keeper_Given()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		Guid keeperId = Guid.NewGuid();

		byte[] opened = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] stored = RandomValues.CreateBytes(16);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(fileId, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = stored,
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(keeperId, ContentIdentity.Contents, stored)
				.Returns(opened);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(fileId, keeperId);

		// Assert
		result
			.Should()
			.Be(new FileContentsReading
			{
				Contents = opened,
				Status = FileContentsStatus.Opened
			});
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(FileDto, CancellationToken)" />: contents that fail their check are
	/// never decrypted.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Reports_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(16),
					IsValid = false
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Damaged);

		contentCipher
			.DidNotReceive()
			.Decrypt(Arg.Any<FileDto>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="FileContentsReader.ReadUnlockedAsync(Guid, Guid?, CancellationToken)" />: contents that fail their check
	/// are never decrypted by the keeper given.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Reports_Damaged_Contents_Before_The_Keeper_Given_Opens_Them()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(fileId, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(16),
					IsValid = false
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileContentsReader sut = mock.Create<FileContentsReader>();

		// Act
		FileContentsReading result = await sut.ReadUnlockedAsync(fileId, Guid.NewGuid());

		// Assert
		result.Status
			.Should()
			.Be(FileContentsStatus.Damaged);

		contentCipher
			.DidNotReceive()
			.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Failures an operation on the key of a session can end with.
	/// </summary>
	private static Exception[] SessionCipherFailures() =>
	[
		new AuthenticationTagMismatchException(),
		new InvalidOperationException()
	];
	#endregion
}
