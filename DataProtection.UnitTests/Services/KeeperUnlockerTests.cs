using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataProtection.Enums;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DataProtection.Services;
using DataProtection.UnitTests.Factories;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using System;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Models;

namespace DataProtection.UnitTests.Services;

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
		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			builder.RegisterInstance(passwordPrompt);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		await sut.RequestDekAsync(
			ItemDtoFactory.CreateKeeperDto(),
			"header",
			"label");

		// Assert
		await passwordPrompt.Received(1).RequestPasswordAsync(
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
		using PinnedBuffer dek = SecretFactory.CreateRandomKey();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(dek);

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(encryption);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			ItemDtoFactory.CreateKeeperDto(),
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
		using PinnedBuffer dek = SecretFactory.CreateRandomKey();

		FolderDto keeper = ItemDtoFactory.CreateKeeperDto();

		byte[]? wrapped = keeper.EncryptedDek;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(dek);

			encryption.RewrapIfOutdated(
				Arg.Any<byte[]>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<ContentIdentity>())!
			.Throws(new InvalidOperationException());

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(encryption);

			builder
				.RegisterType<DekRewrapper>()
				.As<IDekRewrapper>();
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

		keeper.EncryptedDek
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
		FolderDto keeper = ItemDtoFactory.CreateKeeperDto();

		byte[]? wrapped = keeper.EncryptedDek;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(SecretFactory.CreateRandomKey());

			// A null rewrap stands for a wrapper already written at the current cost.
			encryption.RewrapIfOutdated(
				Arg.Any<byte[]>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<ContentIdentity>())
			.Returns((byte[]?)null);

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(encryption);

			builder
				.RegisterType<DekRewrapper>()
				.As<IDekRewrapper>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<KeeperDekWriter>()
				.As<IKeeperDekWriter>();
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		await sut.RequestDekAsync(
			keeper,
			"header");

		// Assert
		keeper.EncryptedDek
			.Should()
			.BeSameAs(wrapped);

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateEncryptedDekAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: the wrapper in memory follows the database,
	/// so a refused write changes nothing.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Keeps_The_Wrapper_When_The_Write_Is_Refused()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateKeeperDto();

		byte[]? wrapped = keeper.EncryptedDek;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(SecretFactory.CreateRandomKey());

			encryption.RewrapIfOutdated(
				Arg.Any<byte[]>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<ContentIdentity>())
			.Returns(RandomValues.CreateBytes(20));

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(encryption);

			builder
				.RegisterType<DekRewrapper>()
				.As<IDekRewrapper>();

			builder
				.RegisterType<KeeperDekWriter>()
				.As<IKeeperDekWriter>();
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		await sut.RequestDekAsync(
			keeper,
			"header");

		// Assert
		keeper.EncryptedDek
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
		IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new PinnedSecret(length: 0));

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(encryption);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			ItemDtoFactory.CreateKeeperDto(),
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
		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			builder.RegisterInstance(passwordPrompt);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			ItemDtoFactory.CreateFolderDto(),
			"header");

		// Assert
		result
			.Should()
			.BeNull();

		await passwordPrompt
			.DidNotReceiveWithAnyArgs()
			.RequestPasswordAsync(default!);
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: a key that cannot be unwrapped is not handed over,
	/// whatever the failure behind it.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(UnwrapFailures))]
	public async Task RequestDekAsync_Returns_Nothing_When_The_Unwrap_Fails(Exception failure)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())!
				.Throws(failure);

			builder.RegisterInstance(passwordPrompt);

			builder.RegisterInstance(encryption);
		});

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			ItemDtoFactory.CreateKeeperDto(),
			"header");

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="KeeperUnlocker.RequestDekAsync" />: an unlock brings the wrapper to the current cost.
	/// </summary>
	[Test]
	public async Task RequestDekAsync_Writes_The_Wrapper_At_The_Current_Cost()
	{
		// Arrange
		using TestDatabase database = new();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey();

		byte[] rewrapped = RandomValues.CreateBytes(20);

		FolderDto keeper = ItemDtoFactory.CreateKeeperDto();

		FolderEntity entity = EntityFactory.CreateFolder(id: keeper.Id, encryptedDek: keeper.EncryptedDek);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(dek);

			encryption.RewrapIfOutdated(
				Arg.Any<byte[]>(),
				Arg.Any<PinnedBuffer>(),
				Arg.Any<PinnedBuffer>(),
				ContentIdentity.Dek)
			.Returns(rewrapped);

			builder.RegisterInstance(database.Context);

			builder.RegisterInstance(encryption);

			builder
				.RegisterType<DekRewrapper>()
				.As<IDekRewrapper>();

			builder.RegisterInstance(passwordPrompt);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();

			builder
				.RegisterType<KeeperDekWriter>()
				.As<IKeeperDekWriter>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([entity]);

		KeeperUnlocker sut = mock.Create<KeeperUnlocker>();

		// Act
		PinnedBuffer? result = await sut.RequestDekAsync(
			keeper,
			"header");

		// Assert
		result
			.Should()
			.BeSameAs(dek);

		keeper.EncryptedDek
			.Should()
			.BeSameAs(rewrapped);

		FolderEntity[] stored = await dbAccess.GetAllFoldersAsync();

		stored
			.Should()
			.ContainSingle()
			.Which
			.EncryptedDek
			.Should()
			.Equal(rewrapped);
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
