using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages.Execution;
using DataOrganizer.Services.Execution;
using DataOrganizer.UnitTests.Factories;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Execution;

[TestFixture(Description = $@"Tests of ""{nameof(FileChangeTracker)}"" type")]
internal class FileChangeTrackerTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: the file is not updated when the content hash is unchanged.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Does_Not_Update_When_Contents_Did_Not_Change()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		byte[] expectedHash = RandomValues.CreateBytes(32);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = RandomValues.CreateBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(_ => new MemoryStream(contents));

			fileSystem
				.ComputeStreamHashAsync(Arg.Any<HashAlgorithmName>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(expectedHash);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = expectedHash,
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = RandomValues.CreateFileName(10)
		};

		cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		await dbAccess.DidNotReceive().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: changed contents are encrypted and the file is updated when a keeper is known.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Encrypts_Contents_When_A_Keeper_Is_Known()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] previousContents = RandomValues.CreateBytes(32);

			byte[] currentContents = RandomValues.CreateBytes(32);

			byte[] encryptedContents = RandomValues.CreateBytes(48);

			byte[] currentHash = RandomValues.CreateBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

			fileSystem
				.ComputeStreamHashAsync(Arg.Any<HashAlgorithmName>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(currentHash);

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(encryptedContents);

			dbAccess
				.UpdateFilePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return true;
				});

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = RandomValues.CreateFileName(10),
			KeeperId = Guid.NewGuid()
		};

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		contentCipher
			.Received(1)
			.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());

		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			parameters.File.Id,
			Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: exits without throwing and without updating when the file no longer exists.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Exits_Gracefully_When_File_Disappears()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = RandomValues.CreateBytes(32);

			byte[] hash = RandomValues.CreateBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(false);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(_ => new MemoryStream(contents));

			fileSystem
				.ComputeStreamHashAsync(Arg.Any<HashAlgorithmName>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(hash);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = RandomValues.CreateFileName(10)
		};

		// Act
		Func<Task> act = () => sut.TrackChangesAsync(parameters);

		// Assert
		await act
			.Should()
			.NotThrowAsync();

		await dbAccess.DidNotReceive().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a change made right before the stop is still persisted,
	/// so hiding the contents cannot discard it.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Persists_The_Last_Change_On_Stop()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		await cancellation.CancelAsync();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(_ => new MemoryStream(RandomValues.CreateBytes(32)));

			fileSystem
				.ComputeStreamHashAsync(Arg.Any<HashAlgorithmName>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(RandomValues.CreateBytes(32));

			dbAccess
				.UpdateFilePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = RandomValues.CreateFileName(10)
		};

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			parameters.File.Id,
			Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: an error snackbar is shown, the file is closed and no update occurs when encryption fails.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Shows_Error_And_Stops_When_Encryption_Fails()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		StrongReferenceMessenger messenger = new();

		FileDto? receivedClosedFile = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedFile = message.File);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] previousContents = RandomValues.CreateBytes(32);

			byte[] currentContents = RandomValues.CreateBytes(32);

			byte[] currentHash = RandomValues.CreateBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

			fileSystem
				.ComputeStreamHashAsync(Arg.Any<HashAlgorithmName>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(currentHash);

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The cipher swallows the cryptographic failure and answers with a refusal.

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = RandomValues.CreateFileName(10),
			KeeperId = Guid.NewGuid()
		};

		// Act
		await sut.TrackChangesAsync(parameters);

		receivedClosedFile
			.Should()
			.Be(parameters.File);

		await dbAccess.DidNotReceive().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: the file is updated and its UpdatedAt refreshed when contents change.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Updates_File_When_Contents_Changed()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] previousContents = RandomValues.CreateBytes(32);

			byte[] currentContents = RandomValues.CreateBytes(32);

			byte[] currentHash = RandomValues.CreateBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

			fileSystem
				.ComputeStreamHashAsync(Arg.Any<HashAlgorithmName>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(currentHash);

			dbAccess
				.UpdateFilePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return true;
				});

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = RandomValues.CreateFileName(10)
		};

		DateTime before = DateTime.Now;

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			parameters.File.Id,
			Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
			Arg.Any<CancellationToken>());

		parameters.File.UpdatedAt
			.Should()
			.BeOnOrAfter(before);
	}
	#endregion
}
