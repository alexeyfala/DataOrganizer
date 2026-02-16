using Autofac;
using Autofac.Extras.Moq;
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
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptAsync_Does_Nothing_If_Db_Returns_Invalid_Contents(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		EncryptDecryptFilesParameters parameters = new()
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
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToLoadContents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptAsync_Does_Nothing_If_Db_Returns_Not_Required_Contents(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		EncryptDecryptFilesParameters parameters = new()
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
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToLoadContents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptAsync_Does_Nothing_If_Encrypted_Contents_Are_Invalid_Or_Have_No_Identifiers(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		EncryptDecryptFilesParameters parameters = new()
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
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToEncryptContents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptAsync_Does_Nothing_If_Encrypted_Not_Required_Contents(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		EncryptDecryptFilesParameters parameters = new()
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
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToEncryptContents);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptAsync_Does_Nothing_If_Failed_To_Save_Contents(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		EncryptDecryptFilesParameters parameters = new()
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
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToSaveContents);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptAsync_Does_Nothing_If_Failed_ToSave_Hash_Of_Password(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		EncryptDecryptFilesParameters parameters = new()
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
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToSavePasswordHash);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptAsync_Does_Nothing_If_Unable_To_Create_Database_Backup(CryptoAction action)
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		EncryptDecryptFilesParameters parameters = new()
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
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.UnableToCreateDatabaseBackup);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptDecryptAsync_Does_Throws_Exception_When_Unsupported_Action_Type()
	{
		// Arrange
		EncryptDecryptFilesParameters parameters = new()
		{
			Action = CryptoAction.ShowFileContents,
			Files = [],
			Folder = TestUtils.CreateFolderDto(),
			Password = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.ExceptionThrown);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptDecryptAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task EncryptDecryptAsync_Successfully_Encrypts_Files(CryptoAction action)
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		EncryptDecryptFilesParameters parameters = new()
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
		FilesEncryptionResult result = await sut.EncryptDecryptAsync(
			mock.Create<EditorViewModel>(),
			parameters);

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.Done);

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

		HandlePasswordInputParameters parameters = new()
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

		HandlePasswordInputParameters parameters = new()
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

		HandlePasswordInputParameters parameters = new()
		{
			Action = CryptoAction.ShowFileContents,
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

		HandlePasswordInputParameters parameters = new()
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

		HandlePasswordInputParameters parameters = new()
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

		HandlePasswordInputParameters parameters = new()
		{
			Action = CryptoAction.ShowFileContents,
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

		folder.EncryptedPassword
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
	/// Test of <see cref="EntityEcryption.HideFolderContents(FolderModelDto)" />.
	/// </summary>
	[Test]
	public void HideFolderContents_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedPassword = TestUtils.CreateRandomBytes(10);

		folder.EncryptionStatus = EncryptionStatus.Decrypted;

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		files.ForEach(x => x.EncryptionStatus = EncryptionStatus.Decrypted);

		folder
			.Children
			.AddRange(files);

		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		sut.HideFolderContents(folder);

		// Assert
		folder.EncryptedPassword
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
	/// Test of <see cref="EntityEcryption.TakePasswordAsync" />.
	/// </summary>
	[Test]
	public async Task TakePasswordAsync_Does_Nothing_If_File_Is_Being_Edited_Or_Executed([Values] CryptoAction action)
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
		await sut.TakePasswordAsync(
			mock.Create<EditorViewModel>(),
			folder,
			action);

		// Assert
		viewFactory
			.Received(0)
			.CreateUserControl<PasswordBox>();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.TakePasswordAsync" />.
	/// </summary>
	[Test]
	public async Task TakePasswordAsync_Does_Nothing_If_Folder_Has_No_Files([Values] CryptoAction action)
	{
		// Arrange
		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>(TypedParameter.From(viewFactory));

		// Act
		await sut.TakePasswordAsync(
			mock.Create<EditorViewModel>(),
			TestUtils.CreateFolderDto(),
			action);

		// Assert
		viewFactory
			.Received(0)
			.CreateUserControl<PasswordBox>();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.TakePasswordAsync" />.
	/// </summary>
	[Test]
	public async Task TakePasswordAsync_Shows_Password_Box([Values] CryptoAction action)
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
		await sut.TakePasswordAsync(
			mock.Create<EditorViewModel>(),
			folder,
			action);

		// Assert
		viewFactory
			.Received()
			.CreateUserControl<PasswordBox>();
	}
	#endregion
}
