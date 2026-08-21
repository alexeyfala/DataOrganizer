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
using Repository.Interfaces;
using System;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(KeeperUnlocker)}"" type")]
internal class KeeperUnlockerTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: the password is only verified, never confirmed.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Asks_To_Verify_The_Password()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		dialogService
			.RequestPasswordAsync(Arg.Any<string>())
			.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		await sut.RequestDekAsync(
			CreateKeeper(),
			"header",
			"label");

		// Assert
		await dialogService.Received(1).RequestPasswordAsync(
			"header",
			"label",
			Arg.Any<string>(),
			PasswordPromptMode.Verify,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: the unwrapped key is handed over to the caller.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Hands_The_Key_Over()
	{
		// Arrange
		using PinnedBuffer dek = SecretUtils.CreateRandomKey();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(dek);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			CreateKeeper(),
			"header");

		// Assert
		result
			.Should()
			.BeSameAs(dek);
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: a rewrap that throws costs neither the key
	/// nor the wrapper.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Hands_The_Key_Over_When_The_Rewrap_Throws()
	{
		// Arrange
		using PinnedBuffer dek = SecretUtils.CreateRandomKey();

		FolderModelDto keeper = CreateKeeper();

		byte[]? wrapped = keeper.EncryptedDek;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = CreateEncryption(dek);

			encryption.RewrapIfOutdated(
				Arg.Any<byte[]>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<ContentIdentity>())!
			.Throws(new InvalidOperationException());

			builder.RegisterInstance(CreateDialogService());

			builder.RegisterInstance(encryption);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			keeper,
			"header");

		// Assert
		result
			.Should()
			.BeSameAs(dek);

		keeper
			.EncryptedDek
			.Should()
			.BeSameAs(wrapped);
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: a wrapper already written at the current cost
	/// is left as it is.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Keeps_A_Wrapper_Of_The_Current_Cost()
	{
		// Arrange
		FolderModelDto keeper = CreateKeeper();

		byte[]? wrapped = keeper.EncryptedDek;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(CreateDialogService());

			builder.RegisterInstance(CreateEncryption(SecretUtils.CreateRandomKey()));

			builder.RegisterInstance(dbAccess);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		await sut.RequestDekAsync(
			keeper,
			"header");

		// Assert
		keeper
			.EncryptedDek
			.Should()
			.BeSameAs(wrapped);

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFolderPropertiesAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: the wrapper in memory follows the database,
	/// so a refused write changes nothing.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Keeps_The_Wrapper_When_The_Write_Is_Refused()
	{
		// Arrange
		FolderModelDto keeper = CreateKeeper();

		byte[]? wrapped = keeper.EncryptedDek;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(CreateDialogService());

			builder.RegisterInstance(CreateEncryption(
				SecretUtils.CreateRandomKey(),
				TestUtils.CreateRandomBytes(20)));
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		await sut.RequestDekAsync(
			keeper,
			"header");

		// Assert
		keeper
			.EncryptedDek
			.Should()
			.BeSameAs(wrapped);
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: a cancelled prompt leaves the wrapped key untouched.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Refuses_A_Cancelled_Prompt()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new PinnedSecret(length: 0));

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			CreateKeeper(),
			"header");

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.DidNotReceiveWithAnyArgs()
			.Decrypt(default!, default!, default);
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: a keeper without a wrapped key is not worth a prompt.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Refuses_A_Keeper_Without_A_Wrapper()
	{
		// Arrange
		IDialogService dialogService = CreateDialogService();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			TestUtils.CreateFolderDto(),
			"header");

		// Assert
		result
			.Should()
			.BeNull();

		await dialogService
			.DidNotReceiveWithAnyArgs()
			.RequestPasswordAsync(default!);
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: a rejected password is reported and nothing is handed over.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(UnwrapFailures))]
	public async Task RequestDekAsync_Reports_A_Failed_Unwrap(Exception failure)
	{
		// Arrange
		IEncryptionFailureReporter failureReporter = Substitute.For<IEncryptionFailureReporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())!
				.Throws(failure);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(failureReporter);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			CreateKeeper(),
			"header");

		// Assert
		result
			.Should()
			.BeNull();

		failureReporter
			.Received(1)
			.Report(failure, Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: an unlock brings the wrapper to the current cost.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Writes_The_Wrapper_At_The_Current_Cost()
	{
		// Arrange
		using PinnedBuffer dek = SecretUtils.CreateRandomKey();

		byte[] rewrapped = TestUtils.CreateRandomBytes(20);

		FolderModelDto keeper = CreateKeeper();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		dbAccess
			.UpdateFolderPropertiesAsync(default, default!, default)
			.ReturnsForAnyArgs(true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(CreateDialogService());

			builder.RegisterInstance(CreateEncryption(dek, rewrapped));

			builder.RegisterInstance(dbAccess);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			keeper,
			"header");

		// Assert
		result
			.Should()
			.BeSameAs(dek);

		keeper
			.EncryptedDek
			.Should()
			.BeSameAs(rewrapped);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a dialog service answering the prompt with a random password.
	/// </summary>
	private static IDialogService CreateDialogService()
	{
		IDialogService dialogService = Substitute.For<IDialogService>();

		dialogService
			.RequestPasswordAsync(Arg.Any<string>())
			.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

		return dialogService;
	}

	/// <summary>
	/// Creates an encryption service unwrapping to <paramref name="dek" /> and rewrapping to
	/// <paramref name="rewrapped" />, where <c>null</c> stands for a wrapper of the current cost.
	/// </summary>
	private static IEncryptionService CreateEncryption(PinnedBuffer dek, byte[]? rewrapped = null)
	{
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		encryption
			.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
			.Returns(dek);

		encryption.RewrapIfOutdated(
			Arg.Any<byte[]>(),
			Arg.Any<PinnedBuffer>(),
			Arg.Any<PinnedBuffer>(),
			Arg.Any<ContentIdentity>())
		.Returns(rewrapped);

		return encryption;
	}

	/// <summary>
	/// Creates a keeper carrying a wrapped key.
	/// </summary>
	private static FolderModelDto CreateKeeper()
	{
		FolderModelDto keeper = TestUtils.CreateFolderDto(
			encryptionStatus: EncryptionStatus.Encrypted);

		keeper.EncryptedDek = TestUtils.CreateRandomBytes(10);

		return keeper;
	}

	/// <summary>
	/// Failures an unwrap can end with.
	/// </summary>
	private static Exception[] UnwrapFailures() =>
	[
		new InvalidCredentialException(),
		new AuthenticationTagMismatchException()
	];
	#endregion
}
