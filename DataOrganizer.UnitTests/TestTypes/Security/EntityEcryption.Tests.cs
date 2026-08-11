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
using Entities.Enums;
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
				.RewrapDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
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
	/// <see cref="EntityEncryption.DecryptFolderAsync" />: the notes of the whole subtree are decrypted and persisted.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Decrypts_Notes()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.Note = TestUtils.CreateRandomBytes(10);

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		FolderModelDto subfolder = TestUtils.CreateFolderDto();

		subfolder.Note = TestUtils.CreateRandomBytes(10);

		folder
			.Children
			.Add(subfolder);

		FileModelDto file = TestUtils.CreateFileDto();

		file.Note = TestUtils.CreateRandomBytes(10);

		FileModelDto[] files = [file];

		byte[] decryptedNote = TestUtils.CreateRandomBytes(10);

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
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.DecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(decryptedNote);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

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

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await dbAccess
			.Received(1)
			.UpdateFolderPropertiesAsync(Arg.Is<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>(x =>
				x != null && x.ContainsKey(folder.Id) && x.ContainsKey(subfolder.Id)));

		folder.Note
			.Should()
			.BeSameAs(decryptedNote);

		subfolder.Note
			.Should()
			.BeSameAs(decryptedNote);

		file.Note
			.Should()
			.BeSameAs(decryptedNote);
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
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
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
	/// <see cref="EntityEncryption.EncryptFolderAsync" />: nothing is persisted when a note cannot be encrypted.
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
				.Returns(AppUtils.CreateRandomString(10).ToCharArray());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

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
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
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
	/// <see cref="EntityEncryption.EncryptFolderAsync" />: the note of an object is encrypted with the DEK of the folder.
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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10).ToCharArray());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(encryptedNote);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

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

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		file.Note
			.Should()
			.BeSameAs(encryptedNote);

		await dbAccess
			.DidNotReceive()
			.UpdateFolderPropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>());
	}

	/// <summary>
	/// <see cref="EntityEncryption.HideFolderContents" />: locks the keeper and marks the folder and all children as encrypted.
	/// </summary>
	[Test]
	public void HideFolderContents_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		folder
			.Children
			.AddRange(TestUtils.CreateFilesDto(5));

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		sut.HideFolderContents(folder);

		// Assert
		sessionKeyStore
			.Received(1)
			.Lock(folder.Id);

		folder.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		folder.GetAllChildren()
			.Should()
			.OnlyContain(x => x.EncryptionStatus == EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// <see cref="EntityEncryption.HideFolderContents" />: hiding a nested folder keeps the key of the keeper
	/// while anything else under it is still shown.
	/// </summary>
	[Test]
	public void HideFolderContents_Keeps_The_Key_While_The_Keeper_Has_Shown_Content()
	{
		// Arrange
		FolderModelDto keeper = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper.EncryptedDek = TestUtils.CreateRandomBytes(10);

		keeper.PasswordHash = AppUtils.CreateRandomString(10);

		FolderModelDto nested = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		nested.Parent = keeper;

		keeper
			.Children
			.Add(nested);

		nested
			.Children
			.AddRange(TestUtils.CreateFilesDto(3, encryptionStatus: EncryptionStatus.Decrypted));

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		sut.HideFolderContents(nested);

		// Assert
		sessionKeyStore
			.DidNotReceive()
			.Lock(Arg.Any<Guid>());

		nested.GetAllChildren()
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

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		sessionKeyStore
			.Unlock(Arg.Any<Guid>(), Arg.Any<byte[]>())
			.Returns(true);

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
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.EncryptWithSessionId(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(sessionKeyStore);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		bool result = await sut.ShowFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeTrue();

		sessionKeyStore
			.Received(1)
			.Unlock(folder.Id, Arg.Any<byte[]>());

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

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		sessionKeyStore
			.Unlock(Arg.Any<Guid>(), Arg.Any<byte[]>())
			.Returns(true);

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
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns([]);

			encryption
				.EncryptWithSessionId(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(sessionKeyStore);
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

		sessionKeyStore
			.Received(1)
			.Unlock(folder.Id, Arg.Any<byte[]>());
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

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.DecryptWithSessionId(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(sessionKeyStore);
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

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.DecryptWithSessionId(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(sessionKeyStore);
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
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
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
			Notes = [],
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
	/// <see cref="EntityEncryption.UpdateDatabaseAsync" />: returns FailedToSaveFolderPropertiesInDb and restores the backup when the notes of the folders cannot be saved.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_Folder_Notes_In_Database()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = null,
			Files = [],
			Folder = folder,
			NewStatus = default,
			Notes = [new NoteUpdate(folder.Id, EntityType.Folder, TestUtils.CreateRandomBytes(10))],
			PasswordHash = null
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
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
			.Be(UpdateDatabaseResult.FailedToSaveFolderPropertiesInDb);

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
			Notes = [],
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
			Notes = [],
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

	/// <summary>
	/// <see cref="EntityEncryption.UpdateDatabaseAsync" />: the processed notes are persisted and applied to the objects.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Saves_Notes()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FolderModelDto subfolder = TestUtils.CreateFolderDto();

		folder
			.Children
			.Add(subfolder);

		FileModelDto file = TestUtils.CreateFileDto();

		byte[] folderNote = TestUtils.CreateRandomBytes(10);

		byte[] subfolderNote = TestUtils.CreateRandomBytes(10);

		byte[] fileNote = TestUtils.CreateRandomBytes(10);

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents =
			[
				new ContentsIsValidPair
				{
					Contents = TestUtils.CreateRandomBytes(10),
					Id = file.Id,
					IsValid = true
				}
			],
			EncryptedDek = TestUtils.CreateRandomBytes(10),
			Files = [file],
			Folder = folder,
			NewStatus = EncryptionStatus.Encrypted,
			Notes =
			[
				new NoteUpdate(folder.Id, EntityType.Folder, folderNote),
				new NoteUpdate(subfolder.Id, EntityType.Folder, subfolderNote),
				new NoteUpdate(file.Id, EntityType.File, fileNote)
			],
			PasswordHash = AppUtils.CreateRandomString(10)
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		EntityEncryption sut = mock.Create<EntityEncryption>();

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.Done);

		await dbAccess
			.Received(1)
			.UpdateFolderPropertiesAsync(Arg.Is<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>(x =>
				x != null && x.ContainsKey(folder.Id) && x.ContainsKey(subfolder.Id) && !x.ContainsKey(file.Id)));

		folder.Note
			.Should()
			.BeSameAs(folderNote);

		subfolder.Note
			.Should()
			.BeSameAs(subfolderNote);

		file.Note
			.Should()
			.BeSameAs(fileNote);
	}
	#endregion
}
