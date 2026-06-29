using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(EntityEncryption)}"" type")]
internal class EntityEncryptionTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityEncryption.ChangePasswordAsync" />: rewraps the DEK and updates the password hash on the folder.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		byte[] encryptedDek = TestUtils.CreateRandomBytes(10);

		string passwordHash = AppUtils.CreateRandomString(10);

		folder.EncryptedDek = encryptedDek;

		folder.PasswordHash = passwordHash;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(AppUtils.CreateRandomString(10).ToCharArray());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.VerifyPassword(Arg.Any<char[]>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.RewrapDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.HashPassword(Arg.Any<char[]>())
				.Returns(AppUtils.CreateRandomString(10));

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		folder.EncryptedDek
			.Should()
			.NotBeEquivalentTo(encryptedDek);

		folder.PasswordHash
			.Should()
			.NotBeEquivalentTo(passwordHash);
	}

	/// <summary>
	/// <see cref="EntityEncryption.DecryptFolderAsync" />: decrypts the folder and persists the updated file properties.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Does_Work()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10).ToCharArray());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.VerifyPassword(Arg.Any<char[]>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.DecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await dbAccess
			.Received()
			.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.DecryptSessionContents" />: returns null when the DEK cannot be decrypted.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Cannot_Decrypt_Binary()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(default(byte[]));

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? result = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10));

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.Received(1)
			.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>());

		encryption
			.Received(1)
			.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.DecryptSessionContents" />: returns null when the encrypted DEK cannot be decrypted.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Cannot_Decrypt_Encrypted_Password()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(default(byte[]));

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? result = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10));

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.Received(1)
			.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.DecryptSessionContents" />: returns non-empty decrypted contents on success.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Does_Work()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? output = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10));

		// Assert
		output
			.Should()
			.NotBeNullOrEmpty();
	}

	/// <summary>
	/// <see cref="EntityEncryption.EncryptFolderAsync" />: encrypts the folder and persists the updated file properties.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Does_Work()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10).ToCharArray());

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		await dbAccess
			.Received()
			.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.EncryptSessionContents" />: returns null and never encrypts when the DEK cannot be decrypted.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Cannot_Decrypt_Encrypted_Password()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(default(byte[]));

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? result = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10));

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.DidNotReceive()
			.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.EncryptSessionContents" />: returns null when encrypting with the DEK fails.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Cannot_Encrypt_Binary()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(default(byte[]));

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? result = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10));

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.Received()
			.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>());

		encryption
			.Received()
			.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.EncryptSessionContents" />: returns non-empty encrypted contents on success.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Does_Work()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? output = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10));

		// Assert
		output
			.Should()
			.NotBeNullOrEmpty();
	}

	/// <summary>
	/// <see cref="EntityEncryption.GetSessionId" />: yields a different value after the session id is reset.
	/// </summary>
	[Test]
	public void GetSessionId_Returns_Different_Values_Between_Sessions()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[] first = sut.GetSessionId();

		sut.ResetSessionId();

		byte[] second = sut.GetSessionId();

		// Assert
		first
			.Should()
			.NotBeEquivalentTo(second);
	}

	/// <summary>
	/// <see cref="EntityEncryption.GetSessionId" />: returns the same value on repeated calls within a session.
	/// </summary>
	[Test]
	public void GetSessionId_Returns_Same_Value_During_Session()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[] value = sut.GetSessionId();

		// Assert
		for (int i = 0; i < 10; i++)
		{
			sut.GetSessionId()
				.Should()
				.BeEquivalentTo(value);
		}
	}

	/// <summary>
	/// <see cref="EntityEncryption.HideFolderContents" />: clears the session DEK and marks the folder and all children as encrypted.
	/// </summary>
	[Test]
	public void HideFolderContents_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		folder
			.Children
			.AddRange(TestUtils.CreateFilesDto(5));

		folder.SessionEncryptedDek = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose();

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		sut.HideFolderContents(folder, []);

		// Assert
		folder.SessionEncryptedDek
			.Should()
			.BeNull();

		folder.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		folder.GetAllChildren()
			.Should()
			.OnlyContain(x => x.EncryptionStatus == EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// <see cref="EntityEncryption.ShowFileContentsAsync" />: sets the session DEK and marks the file as decrypted, returning true.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10).ToCharArray());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.VerifyPassword(Arg.Any<char[]>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		bool result = await sut.ShowFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeTrue();

		folder.SessionEncryptedDek
			.Should()
			.NotBeEmpty();

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// <see cref="EntityEncryption.ShowFolderContentsAsync" />: marks the folder and all children as decrypted and sets the session DEK.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		folder
			.Children
			.AddRange(TestUtils.CreateFilesDto(5, encryptionStatus: EncryptionStatus.Encrypted));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10).ToCharArray());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.VerifyPassword(Arg.Any<char[]>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		await sut.ShowFolderContentsAsync(folder);

		// Assert
		folder.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Decrypted);

		folder.GetAllChildren().Select(x => x.EncryptionStatus)
			.Should()
			.OnlyContain(x => x == EncryptionStatus.Decrypted);

		folder.SessionEncryptedDek
			.Should()
			.NotBeNullOrEmpty();
	}

	/// <summary>
	/// <see cref="EntityEncryption.TryToDecrypt" />: returns non-empty contents that differ from the input.
	/// </summary>
	[Test]
	public void TryToDecrypt_Does_Work()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		folder.SessionEncryptedDek = TestUtils.CreateRandomBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? output = sut.TryToDecrypt(file, contents);

		// Assert
		output
			.Should()
			.NotBeNullOrEmpty();

		output
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// <see cref="EntityEncryption.TryToDecryptContentsAsync" />: decrypts using the session DEK when the file is already decrypted.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Does_Work_When_File_Is_Decrypted()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		folder.SessionEncryptedDek = TestUtils.CreateRandomBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? result = await sut.TryToDecryptContentsAsync(file, contents, string.Empty);

		// Assert
		result
			.Should()
			.NotBeNullOrEmpty();

		result
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// <see cref="EntityEncryption.TryToDecryptContentsAsync" />: prompts for the password and decrypts when the file is encrypted.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Does_Work_When_File_Is_Encrypted()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10).ToCharArray());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.VerifyPassword(Arg.Any<char[]>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? result = await sut.TryToDecryptContentsAsync(file, contents, string.Empty);

		// Assert
		result
			.Should()
			.NotBeNullOrEmpty();

		result
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// <see cref="EntityEncryption.TryToDecryptContentsAsync" />: returns the input unchanged when the file is not encrypted.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Returns_Same_Contents_If_File_Is_Not_Encrypted()
	{
		// Arrange
		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose();

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		byte[]? result = await sut.TryToDecryptContentsAsync(
			TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.None),
			contents,
			string.Empty);

		// Assert
		result
			.Should()
			.BeEquivalentTo(contents);
	}

	/// <summary>
	/// <see cref="EntityEncryption.UpdateDatabaseAsync" />: returns FailedToSaveContentsInDb and restores the backup and erases the file on failure.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_Contents_In_Database()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = null,
			Files = [],
			Folder = TestUtils.CreateFolderDto(),
			NewStatus = default,
			PasswordHash = null
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		EntityEncryption sut = mock.Create<EntityEncryption>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(fileSystem));

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.FailedToSaveContentsInDb);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.UpdateDatabaseAsync" />: returns FailedToSaveFolderPropertiesInDb and restores the backup and erases the file on failure.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_Folder_Properties_In_Database()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = null,
			Files = [],
			Folder = TestUtils.CreateFolderDto(),
			NewStatus = default,
			PasswordHash = null
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.FailedToSaveFolderPropertiesInDb);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.UpdateDatabaseAsync" />: returns Done and applies the new status to the folder and all files.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Does_Work([Values] EncryptionStatus newStatus)
	{
		// Arrange
		EncryptionStatus randomStatus = TestUtils.GetRandomEnumValueExcept(newStatus);

		FolderModelDto folder = TestUtils.CreateFolderDto(encryptionStatus: randomStatus);

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5, encryptionStatus: randomStatus)];

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = TestUtils.CreateRandomBytes(10),
			Files = files,
			Folder = folder,
			NewStatus = newStatus,
			PasswordHash = AppUtils.CreateRandomString(10)
		};

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.Done);

		parameters.Folder.EncryptionStatus
			.Should()
			.Be(newStatus);

		parameters.Files.Select(x => x.EncryptionStatus)
			.Should()
			.OnlyContain(x => x == newStatus);

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}
	#endregion
}
