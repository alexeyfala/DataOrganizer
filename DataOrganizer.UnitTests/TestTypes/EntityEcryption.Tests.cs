using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
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
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EntityEcryption)}"" type")]
internal class EntityEcryptionTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EntityEcryption.ChangePasswordAsync" />.
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
				.RequestUserPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10));

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.RewrapDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(x =>
				{
					x[3] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			encryption
				.EnhancedHashPassword(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10));

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdatePropertiesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<PropertyNameValuePair[]>())
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

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
	/// Test of <see cref="EntityEcryption.DecryptFolderAsync" />.
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
				.RequestUserPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10));

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(true);

			encryption
				.DecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabase()
				.Returns(AppUtils.CreateRandomFileName(10));

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		await sut.DecryptFolderAsync(folder, files);

		// Assert
		await dbAccess
			.Received()
			.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.DecryptSessionContents" />.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Cannot_Decrypt_Binary()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(true, false);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out _);

		// Assert
		result
			.Should()
			.BeFalse();

		encryption.Received(2).Decrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.DecryptSessionContents" />.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Cannot_Decrypt_Encrypted_Password()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(false);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out _);

		// Assert
		result
			.Should()
			.BeFalse();

		encryption.Received(1).Decrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.DecryptSessionContents" />.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Does_Work()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(_ => true, x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out byte[] output);

		// Assert
		result
			.Should()
			.BeTrue();

		output
			.Should()
			.NotBeEmpty();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptFolderAsync" />.
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
				.RequestUserPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10));

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns([.. TestUtils.CreateContents(files.Length, isValid: true)]);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(true);

			dbAccess
				.BackupDatabase()
				.Returns(AppUtils.CreateRandomFileName(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dbAccess);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		await sut.EncryptFolderAsync(folder, files);

		// Assert
		await dbAccess
			.Received()
			.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptSessionContents" />.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Cannot_Decrypt_Encrypted_Password()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(false);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out _);

		// Assert
		result
			.Should()
			.BeFalse();

		encryption.Received(0).Encrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptSessionContents" />.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Cannot_Encrypt_Binary()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(true);

			encryption.Encrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(false);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out _);

		// Assert
		result
			.Should()
			.BeFalse();

		encryption.Received().Encrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());

		encryption.Received().Decrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptSessionContents" />.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Does_Work()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(true);

			encryption.Encrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out byte[] output);

		// Assert
		result
			.Should()
			.BeTrue();

		output
			.Should()
			.NotBeEmpty();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.GetSessionId" />.
	/// </summary>
	[Test]
	public void GetSessionId_Returns_Different_Values_Between_Sessions()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>();

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
	/// Test of <see cref="EntityEcryption.GetSessionId" />.
	/// </summary>
	[Test]
	public void GetSessionId_Returns_Same_Value_During_Session()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>();

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
	/// Test of <see cref="EntityEcryption.HideFolderContents" />.
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

		EntityEcryption sut = mock.Create<EntityEcryption>();

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
	/// Test of <see cref="EntityEcryption.ShowFileContentsAsync" />.
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
				.RequestUserPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10));

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		await sut.ShowFileContentsAsync(file);

		// Assert
		folder.SessionEncryptedDek
			.Should()
			.NotBeEmpty();

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.ShowFolderContentsAsync" />.
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
				.RequestUserPasswordAsync(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10));

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(true);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

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
	/// Test of <see cref="EntityEcryption.TryToDecrypt" />.
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
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.TryToDecrypt(contents, file, out byte[] output);

		// Assert
		result
			.Should()
			.BeTrue();

		output
			.Should()
			.NotBeNullOrEmpty();

		output
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.TryToDecryptContentsAsync" />.
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
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		byte[]? result = await sut.TryToDecryptContentsAsync(file, contents);

		// Assert
		result
			.Should()
			.NotBeNullOrEmpty();

		result
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.TryToDecryptContentsAsync" />.
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
				.RequestUserPasswordAsync(Arg.Any<string>())
				.Returns(string.Empty);

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out Arg.Any<byte[]>())
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		byte[]? result = await sut.TryToDecryptContentsAsync(file, contents);

		// Assert
		result
			.Should()
			.NotBeNullOrEmpty();

		result
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.TryToDecryptContentsAsync" />.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Returns_Same_Contents_If_File_Is_Not_Encrypted()
	{
		// Arrange
		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		byte[]? result = await sut.TryToDecryptContentsAsync(
			TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.None),
			contents);

		// Assert
		result
			.Should()
			.BeEquivalentTo(contents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.UpdateDatabaseAsync" />.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_Contents_In_Database()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = AppUtils.CreateRandomFileName(10),
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

		EntityEcryption sut = mock.Create<EntityEcryption>(
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
	/// Test of <see cref="EntityEcryption.UpdateDatabaseAsync" />.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_Folder_Properties_In_Database()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = AppUtils.CreateRandomFileName(10),
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
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

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
	/// Test of <see cref="EntityEcryption.UpdateDatabaseAsync" />.
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
			BackupFilePath = AppUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = TestUtils.CreateRandomBytes(10),
			Files = files,
			Folder = folder,
			NewStatus = newStatus,
			PasswordHash = AppUtils.CreateRandomString(10)
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>())
				.Returns(true);

			dbAccess
				.UpdatePropertiesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<PropertyNameValuePair[]>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

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
