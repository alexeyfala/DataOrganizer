using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DataProtection.Services;
using DataProtection.UnitTests.Factories;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.Interfaces;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataProtection.UnitTests.Services;

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
		FolderDto keeper = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		FolderDto nested = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		nested.EncryptedDek = RandomValues.CreateBytes(10);

		nested.Parent = keeper;

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

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
	/// <see cref="ContentVisibility.HideAllContents" />: hides every shown object, leaves the rest as it is and drops
	/// every held key.
	/// </summary>
	[Test]
	public void HideAllContents_Hides_The_Shown_Objects_And_Locks_Every_Keeper()
	{
		// Arrange
		FileDto shown = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FileDto plain = ItemDtoFactory.CreateFileDto();

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		sut.HideAllContents([shown, plain]);

		// Assert
		shown.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		plain.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.None);

		sessionKeyStore
			.Received(1)
			.LockAll();
	}

	/// <summary>
	/// <see cref="ContentVisibility.HideFileContents" />: hiding a file keeps the key of its keeper while another file
	/// of the keeper is still shown.
	/// </summary>
	[Test]
	public void HideFileContents_Keeps_The_Key_While_Another_File_Is_Shown()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto hidden = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FileDto shown = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		hidden.Parent = keeper;

		shown.Parent = keeper;

		keeper
			.Children
			.AddRange([hidden, shown]);

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		sut.HideFileContents(hidden);

		// Assert
		hidden.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		sessionKeyStore
			.DidNotReceive()
			.Lock(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="ContentVisibility.HideFileContents" />: hiding the last shown file of a keeper drops the key of the
	/// keeper.
	/// </summary>
	[Test]
	public void HideFileContents_Locks_The_Keeper_Of_The_Last_Shown_File()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		file.Parent = keeper;

		keeper
			.Children
			.Add(file);

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		sut.HideFileContents(file);

		// Assert
		sessionKeyStore
			.Received(1)
			.Lock(keeper.Id);
	}

	/// <summary>
	/// <see cref="ContentVisibility.HideFolderContents" />: hiding a nested folder keeps the key of the keeper
	/// while anything else under it is still shown.
	/// </summary>
	[Test]
	public void HideFolderContents_Keeps_The_Key_While_The_Keeper_Has_Shown_Content()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		FolderDto nested = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		nested.Parent = keeper;

		keeper
			.Children
			.Add(nested);

		nested
			.Children
			.AddRange(ItemDtoFactory.CreateFileDtos(3, encryptionStatus: EncryptionStatus.Decrypted));

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
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		folder
			.Children
			.AddRange(ItemDtoFactory.CreateFileDtos(5));

		folder.EncryptedDek = RandomValues.CreateBytes(10);

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
	/// <see cref="ContentVisibility.ShowFileContentsAsync" />: a key store that refuses the key leaves the file encrypted
	/// and turns the call down.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Keeps_The_File_Encrypted_When_The_Key_Is_Refused()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(false);

			builder.RegisterInstance(unlocker);

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
	/// <see cref="ContentVisibility.ShowFileContentsAsync" />: a cancelled password prompt leaves the file encrypted and
	/// turns the call down.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Turns_Down_A_Cancelled_Password()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns((PinnedBuffer?)null);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		bool result = await sut.ShowFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeFalse();

		sessionKeyStore
			.DidNotReceive()
			.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>());
	}

	/// <summary>
	/// <see cref="ContentVisibility.ShowFileContentsAsync" />: a file outside every keeper is turned down without asking
	/// for a password.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Turns_Down_A_File_Outside_A_Keeper()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(unlocker));

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		bool result = await sut.ShowFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeFalse();

		await unlocker.DidNotReceive().RequestDekAsync(
			Arg.Any<IPasswordKeeper>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ContentVisibility.ShowFileContentsAsync" />: a key the store cannot open leaves the file encrypted and
	/// turns the call down.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Turns_Down_A_Key_That_Cannot_Be_Opened()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Throws(new CryptographicException());

			builder.RegisterInstance(unlocker);

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
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

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

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(10));

			builder.RegisterInstance(unlocker);

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
	/// <see cref="ContentVisibility.ShowFolderContentsAsync" />: a key store that refuses the key leaves the folder encrypted.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Keeps_The_Folder_Encrypted_When_The_Key_Is_Refused()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(false);

			builder.RegisterInstance(unlocker);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentVisibility sut = mock.Create<ContentVisibility>();

		// Act
		await sut.ShowFolderContentsAsync(folder);

		// Assert
		folder
			.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// <see cref="ContentVisibility.ShowFolderContentsAsync" />: unlocks the keeper and marks the folder and all children as decrypted.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Unlocks_The_Keeper_And_Marks_The_Subtree_Decrypted()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.AddRange(ItemDtoFactory.CreateFileDtos(5, encryptionStatus: EncryptionStatus.Encrypted));

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			sessionKeyStore
				.Unlock(Arg.Any<Guid>(), Arg.Any<PinnedBuffer>())
				.Returns(true);

			IKeeperUnlocker unlocker = Substitute.For<IKeeperUnlocker>();

			unlocker
				.RequestDekAsync(
					Arg.Any<IPasswordKeeper>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<string>())
				.Returns(SecretFactory.CreateRandomKey(32));

			builder.RegisterInstance(unlocker);

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
}
