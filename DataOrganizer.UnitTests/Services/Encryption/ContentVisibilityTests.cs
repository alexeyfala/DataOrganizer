using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Encryption;

[TestFixture(Description = $@"Tests of ""{nameof(ContentVisibility)}"" type")]
internal class ContentVisibilityTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentVisibility.DiscardAllKeys" />: drops every held key.
	/// </summary>
	[Test]
	public void DiscardAllKeys_Locks_Every_Keeper()
	{
		// Arrange
		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		sut.DiscardAllKeys();

		// Assert
		sessionKeyStore
			.Received(1)
			.LockAll();
	}

	/// <summary>
	/// <see cref="ContentVisibility.DiscardKeys" />: drops the keys of the folder and of every folder beneath it,
	/// even while their contents are shown.
	/// </summary>
	[Test]
	public void DiscardKeys_Locks_The_Folder_And_Its_Nested_Keepers()
	{
		// Arrange
		FolderDto keeper = TestData.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper.EncryptedDek = TestData.CreateRandomBytes(10);

		FolderDto nested = TestData.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		nested.EncryptedDek = TestData.CreateRandomBytes(10);

		nested.Parent = keeper;

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		nested
			.Children
			.Add(file);

		keeper
			.Children
			.Add(nested);

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		sut.DiscardKeys(keeper);

		// Assert
		sessionKeyStore
			.Received(1)
			.Lock(keeper.Id);

		sessionKeyStore
			.Received(1)
			.Lock(nested.Id);

		sessionKeyStore
			.DidNotReceive()
			.Lock(file.Id);
	}

	/// <summary>
	/// <see cref="ContentVisibility.HideFolderContents" />: hiding a nested folder keeps the key of the keeper
	/// while anything else under it is still shown.
	/// </summary>
	[Test]
	public void HideFolderContents_Keeps_The_Key_While_The_Keeper_Has_Shown_Content()
	{
		// Arrange
		FolderDto keeper = TestData.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper.EncryptedDek = TestData.CreateRandomBytes(10);

		FolderDto nested = TestData.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		nested.Parent = keeper;

		keeper
			.Children
			.Add(nested);

		nested
			.Children
			.AddRange(TestData.CreateFileDtos(3, encryptionStatus: EncryptionStatus.Decrypted));

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
	/// <see cref="ContentVisibility.HideFolderContents" />: locks the keeper and marks the folder and all children as encrypted.
	/// </summary>
	[Test]
	public void HideFolderContents_Locks_The_Keeper_And_Marks_The_Subtree_Encrypted()
	{
		// Arrange
		FolderDto folder = TestData.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		folder
			.Children
			.AddRange(TestData.CreateFileDtos(5));

		folder.EncryptedDek = TestData.CreateRandomBytes(10);

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
	/// <see cref="ContentVisibility.ShowFileContentsAsync" />: reports a refused key instead of returning silently.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Reports_A_Refused_Key()
	{
		// Arrange
		FolderDto folder = TestData.CreateFolderDto();

		folder.EncryptedDek = TestData.CreateRandomBytes(10);

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			RegisterUnlocker(builder, SecretFactory.CreateRandomKey(32));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(false);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		bool result = await sut.ShowFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeFalse();

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// <see cref="ContentVisibility.ShowFileContentsAsync" />: unlocks the keeper and marks the file as decrypted, returning true.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Unlocks_The_Keeper_And_Marks_The_File_Decrypted()
	{
		// Arrange
		FolderDto folder = TestData.CreateFolderDto();

		folder.EncryptedDek = TestData.CreateRandomBytes(10);

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

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
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			RegisterUnlocker(builder, SecretFactory.CreateRandomKey(10));

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
	/// <see cref="ContentVisibility.ShowFolderContentsAsync" />: a key store that refuses the key is
	/// reported as a failure to show the contents.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Reports_A_Refused_Key()
	{
		// Arrange
		FolderDto folder = TestData.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = TestData.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			RegisterUnlocker(builder, SecretFactory.CreateRandomKey(32));

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(false);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		await sut.ShowFolderContentsAsync(folder);
	}

	/// <summary>
	/// <see cref="ContentVisibility.ShowFolderContentsAsync" />: unlocks the keeper and marks the folder and all children as decrypted.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Unlocks_The_Keeper_And_Marks_The_Subtree_Decrypted()
	{
		// Arrange
		FolderDto folder = TestData.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = TestData.CreateRandomBytes(10);

		folder
			.Children
			.AddRange(TestData.CreateFileDtos(5, encryptionStatus: EncryptionStatus.Encrypted));

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(true);

			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(SecretFactory.CreateRandomSecret());

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			RegisterUnlocker(builder, SecretFactory.CreateRandomKey(32));

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
