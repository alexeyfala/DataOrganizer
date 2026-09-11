using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Encryption;

[TestFixture(Description = $@"Tests of ""{nameof(ContentCipher)}"" type")]
internal class ContentCipherTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentCipher.Decrypt" />: empty contents are stored unencrypted, so they come
	/// back untouched and the key store stays out of it.
	/// </summary>
	[Test]
	public void Decrypt_Hands_Empty_Contents_Back()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

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
	/// <see cref="ContentCipher.Decrypt" />: returns non-empty contents that differ from the input.
	/// </summary>
	[Test]
	public void Decrypt_Returns_Contents_Different_From_The_Input()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(RandomValues.CreateBytes(10));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(RandomValues.CreateBytes(10));

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
	/// <see cref="ContentCipher.TryDecrypt" />: hands the plain text of the key store over.
	/// </summary>
	[Test]
	public void TryDecrypt_Hands_The_Plain_Text_Over()
	{
		// Arrange
		Guid keeperId = Guid.NewGuid();

		byte[] input = RandomValues.CreateBytes(10);

		byte[] decrypted = RandomValues.CreateBytes(10);

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
			RandomValues.CreateBytes(10));

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ContentCipher.TryDecryptContentsAsync" />: prompts for the password and decrypts when the file is encrypted.
	/// </summary>
	[Test]
	public async Task TryDecryptContentsAsync_Asks_For_The_Password_When_File_Is_Encrypted()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			RegisterUnlocker(builder, SecretFactory.CreateRandomKey(10));

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = await sut.TryDecryptContentsAsync(file, contents, string.Empty);

		// Assert
		result
			.Should()
			.NotBeNullOrEmpty();

		result
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// <see cref="ContentCipher.TryDecryptContentsAsync" />: decrypts through the key store when the file is already decrypted.
	/// </summary>
	[Test]
	public async Task TryDecryptContentsAsync_Decrypts_Through_The_Key_Store_When_File_Is_Decrypted()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(RandomValues.CreateBytes(10));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = await sut.TryDecryptContentsAsync(file, contents, string.Empty);

		// Assert
		result
			.Should()
			.NotBeNullOrEmpty();

		result
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// <see cref="ContentCipher.TryDecryptContentsAsync" />: a file belonging to no password keeper
	/// cannot be decrypted, so no password is asked for.
	/// </summary>
	[Test]
	public async Task TryDecryptContentsAsync_Does_Not_Ask_For_A_Password_Without_A_Keeper()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = await sut.TryDecryptContentsAsync(
			file,
			RandomValues.CreateBytes(10),
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
	/// <see cref="ContentCipher.TryDecryptContentsAsync" />: empty contents come back untouched
	/// and no password is asked for.
	/// </summary>
	[Test]
	public async Task TryDecryptContentsAsync_Hands_Empty_Contents_Back()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = await sut.TryDecryptContentsAsync(
			ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted),
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
	/// <see cref="ContentCipher.TryDecryptContentsAsync" />: returns the input unchanged when the file is not encrypted.
	/// </summary>
	[Test]
	public async Task TryDecryptContentsAsync_Returns_Same_Contents_If_File_Is_Not_Encrypted()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose();

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = await sut.TryDecryptContentsAsync(
			ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.None),
			contents,
			string.Empty);

		// Assert
		result
			.Should()
			.BeEquivalentTo(contents);
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
			RandomValues.CreateBytes(10));

		// Assert
		result
			.Should()
			.BeNull();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Registers an unlocker that hands the key over without a prompt; <c>null</c> stands for a refusal.
	/// </summary>
	private static IKeeperUnlocker RegisterUnlocker(ContainerBuilder builder, PinnedBuffer? dek)
	{
		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		unlocker
			.RequestDekAsync(
				Arg.Any<IPasswordKeeper>(),
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
