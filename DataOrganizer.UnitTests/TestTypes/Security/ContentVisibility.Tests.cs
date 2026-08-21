using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(ContentVisibility)}"" type")]
internal class ContentVisibilityTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentVisibility.HideFolderContents" />: locks the keeper and marks the folder and all children as encrypted.
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

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		ContentVisibility sut = mock.Create<ContentVisibility>();

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
	/// <see cref="ContentVisibility.HideFolderContents" />: hiding a nested folder keeps the key of the keeper
	/// while anything else under it is still shown.
	/// </summary>
	[Test]
	public void HideFolderContents_Keeps_The_Key_While_The_Keeper_Has_Shown_Content()
	{
		// Arrange
		FolderModelDto keeper = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper.EncryptedDek = TestUtils.CreateRandomBytes(10);

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

		ContentVisibility sut = mock.Create<ContentVisibility>();

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
	/// <see cref="ContentVisibility.ShowFileContentsAsync" />: unlocks the keeper and marks the file as decrypted, returning true.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(true);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			RegisterUnlocker(builder, SecretUtils.CreateRandomKey(10));

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		bool result = await sut.ShowFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeTrue();

		sessionKeyStore
			.Received(1)
			.Unlock(folder.Id, Arg.Any<PinnedBuffer>());

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// <see cref="ContentVisibility.ShowFileContentsAsync" />: reports a refused key instead of returning silently.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Reports_A_Refused_Key()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? received = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => received = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			RegisterUnlocker(builder, SecretUtils.CreateRandomKey(32));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(false);

			builder.RegisterInstance(sessionKeyStore);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		bool result = await sut.ShowFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeFalse();

		received
			.Should()
			.NotBeNull();

		received
			.Text
			.Should()
			.Be(Strings.FailedToShowFileContents);

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// <see cref="ContentVisibility.ShowFolderContentsAsync" />: unlocks the keeper and marks the folder and all children as decrypted.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		folder
			.Children
			.AddRange(TestUtils.CreateFilesDto(5, encryptionStatus: EncryptionStatus.Encrypted));

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(true);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretUtils.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			RegisterUnlocker(builder, SecretUtils.CreateRandomKey(32));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentVisibility sut = mock.Create<ContentVisibility>();

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
			.Unlock(folder.Id, Arg.Any<PinnedBuffer>());
	}

	/// <summary>
	/// <see cref="ContentVisibility.ShowFolderContentsAsync" />: a key store that refuses the key is
	/// reported as a failure to show the contents.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Reports_A_Refused_Key()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? received = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => received = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			RegisterUnlocker(builder, SecretUtils.CreateRandomKey(32));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(false);

			builder.RegisterInstance(sessionKeyStore);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		await sut.ShowFolderContentsAsync(folder);

		// Assert
		received
			.Should()
			.NotBeNull();

		received
			.Text
			.Should()
			.Be(Strings.FailedToShowFileContents);

		received
			.Level
			.Should()
			.Be(SnackbarMessageLevel.Error);
	}

	#endregion

	#region Helpers
	/// <summary>
	/// Registers an unlocker that hands the key over without a prompt; <c>null</c> stands for a refusal.
	/// </summary>
	private static IKeeperUnlocker RegisterUnlocker(ContainerBuilder builder, PinnedBuffer? dek)
	{
		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		unlocker.RequestDekAsync(
			Arg.Any<IPasswordKeeper>(),
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
