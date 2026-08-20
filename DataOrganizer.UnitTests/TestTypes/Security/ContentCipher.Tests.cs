using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(ContentCipher)}"" type")]
internal class ContentCipherTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentCipher.Decrypt" />: returns non-empty contents that differ from the input.
	/// </summary>
	[Test]
	public void Decrypt_Does_Work()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<ContentIdentity>())
				.Returns(TestUtils.CreateRandomBytes(10));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[] output = sut.Decrypt(file, contents);

		// Assert
		output
			.Should()
			.NotBeNullOrEmpty();

		output
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// <see cref="ContentCipher.Decrypt" />: empty contents are stored unencrypted, so they come
	/// back untouched and the key store stays out of it.
	/// </summary>
	[Test]
	public void Decrypt_Hands_Empty_Contents_Back()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[] output = sut.Decrypt(file, []);

		// Assert
		output
			.Should()
			.BeEmpty();

		sessionKeyStore
			.DidNotReceiveWithAnyArgs()
			.Decrypt(default, default, default!);
	}

	/// <summary>
	/// <see cref="ContentCipher.TryDecrypt" />: hands the plain text of the key store over.
	/// </summary>
	[Test]
	public void TryDecrypt_Hands_The_Plain_Text_Over()
	{
		// Arrange
		Guid keeperId = Guid.NewGuid();

		byte[] input = TestUtils.CreateRandomBytes(10);

		byte[] decrypted = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(keeperId, Arg.Any<ContentIdentity>(), input)
				.Returns(decrypted);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = sut.TryDecrypt(keeperId, ContentIdentity.ForNote(Guid.NewGuid()), input);

		// Assert
		result
			.Should()
			.BeSameAs(decrypted);
	}

	/// <summary>
	/// <see cref="ContentCipher.TryDecrypt" />: a locked keeper or damaged data ends with a refusal
	/// instead of an exception, since the caller renders the content.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(SessionCipherFailures))]
	public void TryDecrypt_Refuses_On_A_Failure(Exception failure)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Throws(failure);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = sut.TryDecrypt(
			Guid.NewGuid(),
			ContentIdentity.ForNote(Guid.NewGuid()),
			TestUtils.CreateRandomBytes(10));

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ContentCipher.TryEncrypt" />: a locked keeper ends with a refusal instead of an exception.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(SessionCipherFailures))]
	public void TryEncrypt_Refuses_On_A_Failure(Exception failure)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Encrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Throws(failure);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = sut.TryEncrypt(
			Guid.NewGuid(),
			ContentIdentity.ForNote(Guid.NewGuid()),
			TestUtils.CreateRandomBytes(10));

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ContentCipher.TryToDecryptContentsAsync" />: a file belonging to no password keeper
	/// cannot be decrypted, so no password is asked for.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Does_Not_Ask_For_A_Password_Without_A_Keeper()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = await sut.TryToDecryptContentsAsync(
			file,
			TestUtils.CreateRandomBytes(10),
			string.Empty);

		// Assert
		result
			.Should()
			.BeNull();

		await dialogService
			.DidNotReceiveWithAnyArgs()
			.RequestPasswordAsync(default!);
	}

	/// <summary>
	/// <see cref="ContentCipher.TryToDecryptContentsAsync" />: decrypts through the key store when the file is already decrypted.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Does_Work_When_File_Is_Decrypted()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<ContentIdentity>())
				.Returns(TestUtils.CreateRandomBytes(10));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

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
	/// <see cref="ContentCipher.TryToDecryptContentsAsync" />: prompts for the password and decrypts when the file is encrypted.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Does_Work_When_File_Is_Encrypted()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

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
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			RegisterUnlocker(builder, TestUtils.CreateRandomBytes(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<ContentIdentity>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

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
	/// <see cref="ContentCipher.TryToDecryptContentsAsync" />: empty contents come back untouched
	/// and no password is asked for.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Hands_Empty_Contents_Back()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = await sut.TryToDecryptContentsAsync(
			TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted),
			[],
			string.Empty);

		// Assert
		result
			.Should()
			.BeEmpty();

		await dialogService
			.DidNotReceiveWithAnyArgs()
			.RequestPasswordAsync(default!);
	}

	/// <summary>
	/// <see cref="ContentCipher.TryToDecryptContentsAsync" />: returns the input unchanged when the file is not encrypted.
	/// </summary>
	[Test]
	public async Task TryToDecryptContentsAsync_Returns_Same_Contents_If_File_Is_Not_Encrypted()
	{
		// Arrange
		byte[] contents = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose();

		ContentCipher sut = mock.Create<ContentCipher>();

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
	#endregion

	#region Helpers
	/// <summary>
	/// Registers an unlocker that hands the key over without a prompt; <c>null</c> stands for a refusal.
	/// </summary>
	private static IKeeperUnlocker RegisterUnlocker(ContainerBuilder builder, byte[]? dek)
	{
		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		unlocker
			.RequestDekAsync(
				Arg.Any<FolderModelDto>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>(),
				Arg.Any<string>())
			.Returns(dek);

		builder.RegisterInstance(unlocker);

		return unlocker;
	}

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
