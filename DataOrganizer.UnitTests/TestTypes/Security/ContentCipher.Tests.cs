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
using System;
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
				Arg.Any<Guid>(),
				Arg.Any<byte[]>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>(),
				Arg.Any<string>())
			.Returns(dek);

		builder.RegisterInstance(unlocker);

		return unlocker;
	}
	#endregion
}
