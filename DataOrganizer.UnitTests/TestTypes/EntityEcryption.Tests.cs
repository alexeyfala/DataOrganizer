using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
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

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EntityEcryption)}"" type")]
internal class EntityEcryptionTests
{
	#region Methods
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
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptFolderAsync_Does_Nothing_If_Db_Returns_Invalid_Contents(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		FolderEncryptionParameters parameters = new()
		{
			Action = action,
			Files = files,
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: false).ToAsyncEnumerable());

			builder.RegisterInstance(dbAccess);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FolderEncryptionResult.FailedToLoadContents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptFolderAsync_Does_Nothing_If_Db_Returns_Not_Required_Contents(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		FolderEncryptionParameters parameters = new()
		{
			Action = action,
			Files = files,
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length - 2, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(dbAccess);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FolderEncryptionResult.FailedToLoadContents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptFolderAsync_Does_Nothing_If_Encrypted_Contents_Are_Invalid_Or_Have_No_Identifiers(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		FolderEncryptionParameters parameters = new()
		{
			Action = action,
			Files = files,
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			encryption
				.EncryptDecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>(), Arg.Any<CryptoAction>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: false, generateId: false));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		switch (action)
		{
			case CryptoAction.Encrypt:
				result
					.Should()
					.Be(FolderEncryptionResult.FailedToEncryptContents);
				break;

			case CryptoAction.Decrypt:
				result
					.Should()
					.Be(FolderEncryptionResult.FailedToDecryptContents);
				break;

			default:
				throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptFolderAsync_Does_Nothing_If_Encrypted_Not_Required_Contents(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		FolderEncryptionParameters parameters = new()
		{
			Action = action,
			Files = files,
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			encryption
				.EncryptDecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>(), Arg.Any<CryptoAction>())
				.Returns(TestUtils.CreateContents(files.Length - 2, isValid: true));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		switch (action)
		{
			case CryptoAction.Encrypt:
				result
					.Should()
					.Be(FolderEncryptionResult.FailedToEncryptContents);
				break;

			case CryptoAction.Decrypt:
				result
					.Should()
					.Be(FolderEncryptionResult.FailedToDecryptContents);
				break;

			default:
				throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptFolderAsync_Does_Nothing_If_Failed_To_Save_Contents(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		FolderEncryptionParameters parameters = new()
		{
			Action = action,
			Files = files,
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabase(out _)
				.Returns(x =>
				{
					x[0] = AppUtils.CreateRandomFileName(10);

					return true;
				});

			dbAccess
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>())
				.Returns(false);

			encryption
				.EncryptDecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>(), Arg.Any<CryptoAction>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(fileSystem);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FolderEncryptionResult.FailedToSaveContents);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptFolderAsync_Does_Nothing_If_Failed_ToSave_Hash_Of_Password(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		FolderEncryptionParameters parameters = new()
		{
			Action = action,
			Files = files,
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabase(out _)
				.Returns(x =>
				{
					x[0] = AppUtils.CreateRandomFileName(10);

					return true;
				});

			dbAccess
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>())
				.Returns(true);

			encryption
				.EncryptDecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>(), Arg.Any<CryptoAction>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(fileSystem);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FolderEncryptionResult.FailedToSavePasswordHash);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptFolderAsync_Does_Nothing_If_Unable_To_Create_Database_Backup(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		FolderEncryptionParameters parameters = new()
		{
			Action = action,
			Files = files,
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.BackupDatabase(out _)
				.Returns(false);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			encryption
				.EncryptDecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>(), Arg.Any<CryptoAction>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FolderEncryptionResult.UnableToCreateDatabaseBackup);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptDecryptFolderAsync_Does_Throws_Exception_When_Unsupported_Action_Type()
	{
		// Arrange
		FolderEncryptionParameters parameters = new()
		{
			Action = CryptoAction.ShowFolderContents,
			Files = [],
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FolderEncryptionResult.ExceptionThrown);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptFolderAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptFolderAsync_Successfully_Encrypts_Files(CryptoAction action)
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		FolderEncryptionParameters parameters = new()
		{
			Action = action,
			Files = files,
			Folder = folder,
			Password = AppUtils.CreateRandomString(10)
		};

		EncryptionStatus newStatus = action switch
		{
			CryptoAction.Encrypt => EncryptionStatus.Encrypted,
			CryptoAction.Decrypt => EncryptionStatus.None,
			_ => throw new NotImplementedException()
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabase(out _)
				.Returns(x =>
				{
					x[0] = AppUtils.CreateRandomFileName(10);

					return true;
				});

			dbAccess
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>())
				.Returns(true);

			dbAccess
				.UpdatePropertyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.EncryptDecryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>(), Arg.Any<CryptoAction>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true));

			string? passwordHash = action switch
			{
				CryptoAction.Encrypt => AppUtils.CreateRandomString(10),
				CryptoAction.Decrypt => null,
				_ => throw new NotImplementedException()
			};

			encryption
				.EnhancedHashPassword(Arg.Any<string>())
				.Returns(passwordHash);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(fileSystem);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FolderEncryptionResult result = await sut.EncryptDecryptFolderAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FolderEncryptionResult.Done);

		folder.EncryptionStatus
			.Should()
			.Be(newStatus);

		files
			.Should()
			.OnlyContain(x => x.EncryptionStatus == newStatus);

		switch (action)
		{
			case CryptoAction.Encrypt:
				folder.PasswordHash
					.Should()
					.NotBeNullOrEmpty();
				break;

			case CryptoAction.Decrypt:
				folder.PasswordHash
					.Should()
					.BeNull();
				break;
		}

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
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
	/// Test of <see cref="EntityEcryption.HandlePasswordInputAsync" />.
	/// </summary>
	[Test]
	public async Task HandlePasswordInputAsync_Allows_To_Decrypt_When_Password_Hash_Missing()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HandlePasswordParameters parameters = new()
		{
			Action = CryptoAction.Decrypt,
			Files = [],
			Folder = TestUtils.CreateFolderDto()
		};

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		HandlePasswordResult result = await sut.HandlePasswordInputAsync(
			AppUtils.CreateRandomString(10),
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(HandlePasswordResult.Applied);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HandlePasswordInputAsync" />.
	/// </summary>
	[Test]
	public async Task HandlePasswordInputAsync_Allows_To_Encrypt()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HandlePasswordParameters parameters = new()
		{
			Action = CryptoAction.Encrypt,
			Files = [],
			Folder = TestUtils.CreateFolderDto()
		};

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		HandlePasswordResult result = await sut.HandlePasswordInputAsync(
			AppUtils.CreateRandomString(10),
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(HandlePasswordResult.Applied);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HandlePasswordInputAsync" />.
	/// </summary>
	[Test]
	public async Task HandlePasswordInputAsync_Cannot_Show_File_Contents()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			builder.RegisterInstance(encryption);
		});

		HandlePasswordParameters parameters = new()
		{
			Action = CryptoAction.ShowFolderContents,
			Files = [],
			Folder = folder
		};

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		HandlePasswordResult result = await sut.HandlePasswordInputAsync(
			AppUtils.CreateRandomString(10),
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(HandlePasswordResult.FailedToShowFileContents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HandlePasswordInputAsync" />.
	/// </summary>
	[Test]
	public async Task HandlePasswordInputAsync_Does_Nothing_If_Password_Hash_Does_Not_Match()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(false);

			builder.RegisterInstance(encryption);
		});

		HandlePasswordParameters parameters = new()
		{
			Action = CryptoAction.Decrypt,
			Files = [],
			Folder = folder
		};

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		HandlePasswordResult result = await sut.HandlePasswordInputAsync(
			AppUtils.CreateRandomString(10),
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(HandlePasswordResult.PasswordDoesNotMatch);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HandlePasswordInputAsync" />.
	/// </summary>
	[Test]
	public async Task HandlePasswordInputAsync_Does_Nothing_If_Password_Not_Entered()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HandlePasswordParameters parameters = new()
		{
			Action = default,
			Files = [],
			Folder = TestUtils.CreateFolderDto()
		};

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		HandlePasswordResult result = await sut.HandlePasswordInputAsync(
			null,
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(HandlePasswordResult.PasswordNotEntered);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HandlePasswordInputAsync" />.
	/// </summary>
	[Test]
	public async Task HandlePasswordInputAsync_Shows_File_Contents()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		folder
			.Children
			.AddRange(TestUtils.CreateFilesDto(5));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out _)
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);
		});

		HandlePasswordParameters parameters = new()
		{
			Action = CryptoAction.ShowFolderContents,
			Files = [],
			Folder = folder
		};

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		HandlePasswordResult result = await sut.HandlePasswordInputAsync(
			AppUtils.CreateRandomString(10),
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(HandlePasswordResult.Applied);

		folder.SessionEncryptedDek
			.Should()
			.NotBeEmpty();

		folder.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Decrypted);

		folder.Children
			.Should()
			.OnlyContain(x => x.EncryptionStatus == EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HideFileContentsAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task HideFileContentsAsync_Does_Work([Values] bool isEdited)
	{
		// Arrange
		FileModelDto file = isEdited
			? TestUtils.CreateFileDto(isEdited: true)
			: TestUtils.CreateFileDto(isExecuted: true);

		file.EncryptionStatus = EncryptionStatus.Decrypted;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<EditFilesView>()
				.Returns(mock.Create<EditFilesView>());

			YesNoCancelBox view = mock.Create<YesNoCancelBox>();

			viewFactory
				.CreateUserControl<YesNoCancelBox>()
				.Returns(view);

			builder.RegisterInstance(viewFactory);

			_ = view
				.ViewModel
				.SetResultAsync(YesNoCancelResult.Yes);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		await sut.HideFileContentsAsync(file, mock.Create<EditorViewModel>());

		// Assert
		file.IsEdited
			.Should()
			.BeFalse();

		file.IsExecuted
			.Should()
			.BeFalse();

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HideFolderContentsAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task HideFolderContentsAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] editedFiles = [.. TestUtils.CreateFilesDto(5, isEdited: true)];

		FileModelDto[] executedFiles = [.. TestUtils.CreateFilesDto(5, isExecuted: true)];

		folder
			.Children
			.AddRange(editedFiles.Concat(executedFiles));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<EditFilesView>()
				.Returns(mock.Create<EditFilesView>());

			YesNoCancelBox view = mock.Create<YesNoCancelBox>();

			viewFactory
				.CreateUserControl<YesNoCancelBox>()
				.Returns(view);

			builder.RegisterInstance(viewFactory);

			_ = view
				.ViewModel
				.SetResultAsync(YesNoCancelResult.Yes);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		await sut.HideFolderContentsAsync(folder, mock.Create<EditorViewModel>());

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

		editedFiles
			.Should()
			.OnlyContain(x => !x.IsEdited);

		executedFiles
			.Should()
			.OnlyContain(x => !x.IsExecuted);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.RequestPasswordAsync" />.
	/// </summary>
	[Test]
	public async Task RequestPasswordAsync_Does_Nothing_If_File_Is_Being_Edited_Or_Executed([Values] CryptoAction action)
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files =
		[
			.. TestUtils.CreateFilesDto(5, isEdited: true),
			.. TestUtils.CreateFilesDto(5, isExecuted: true)
		];

		folder
			.Children
			.AddRange(files);

		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>(TypedParameter.From(viewFactory));

		// Act
		await sut.RequestPasswordAsync(
			mock.Create<EditorViewModel>(),
			folder,
			action);

		// Assert
		viewFactory
			.Received(0)
			.CreateUserControl<PasswordBox>();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.RequestPasswordAsync" />.
	/// </summary>
	[Test]
	public async Task RequestPasswordAsync_Does_Nothing_If_Folder_Has_No_Files([Values] CryptoAction action)
	{
		// Arrange
		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>(TypedParameter.From(viewFactory));

		// Act
		await sut.RequestPasswordAsync(
			mock.Create<EditorViewModel>(),
			TestUtils.CreateFolderDto(),
			action);

		// Assert
		viewFactory
			.Received(0)
			.CreateUserControl<PasswordBox>();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.RequestPasswordAsync" />.
	/// </summary>
	[Test]
	public async Task RequestPasswordAsync_Shows_Password_Box([Values] CryptoAction action)
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(TestUtils.CreateFilesDto(5));

		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>(TypedParameter.From(viewFactory));

		// Act
		await sut.RequestPasswordAsync(
			mock.Create<EditorViewModel>(),
			folder,
			action);

		// Assert
		viewFactory
			.Received()
			.CreateUserControl<PasswordBox>();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.ShowFileContentsAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowFileContentsAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		FileModelDto file = TestUtils.CreateFileDto();

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			PasswordBox view = mock.Create<PasswordBox>();

			view
				.ViewModel
				.Password = AppUtils.CreateRandomString(10);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<PasswordBox>()
				.Returns(view);

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out _)
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(encryption);

			_ = view
				.ViewModel
				.SetResultAsync(true);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		await sut.ShowFileContentsAsync(file, mock.Create<EditorViewModel>());

		// Assert
		folder.SessionEncryptedDek
			.Should()
			.NotBeEmpty();

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Decrypted);
	}
	#endregion
}
