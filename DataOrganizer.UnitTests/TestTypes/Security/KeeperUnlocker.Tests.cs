using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Enums;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
			Guid.NewGuid(),
			TestUtils.CreateRandomBytes(10),
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
		byte[] dek = TestUtils.CreateRandomBytes(32);

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
		byte[]? result = await sut.RequestDekAsync(
			Guid.NewGuid(),
			TestUtils.CreateRandomBytes(10),
			"header");

		// Assert
		result
			.Should()
			.BeSameAs(dek);
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
		byte[]? result = await sut.RequestDekAsync(
			Guid.NewGuid(),
			TestUtils.CreateRandomBytes(10),
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
		byte[]? result = await sut.RequestDekAsync(
			Guid.NewGuid(),
			TestUtils.CreateRandomBytes(10),
			"header");

		// Assert
		result
			.Should()
			.BeNull();

		failureReporter
			.Received(1)
			.Report(failure, Arg.Any<string>());
	}
	#endregion

	#region Helpers
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
