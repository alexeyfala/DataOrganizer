using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.ViewModels;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.Interfaces;
using Shared.Common;
using Shared.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FileChangeTracker)}"" type")]
internal class FileChangeTrackerTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="FileChangeTracker.TrackChangesAsync" />.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Does_Not_Update_When_Contents_Did_Not_Change()
	{
		// Arrange
		using CancellationTokenSource cts = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = TestUtils.CreateRandomBytes(32);

			byte[] hash = TestUtils.CreateRandomBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(_ => new MemoryStream(contents));

			fileSystem
				.ComputeSha256HashAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(hash);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			Contents = TestUtils.CreateRandomBytes(10),
			File = TestUtils.CreateFileDto(),
			FileName = AppUtils.CreateRandomFileName(10),
			FilePath = AppUtils.CreateRandomFileName(10),
			SessionEncryptedDek = TestUtils.CreateRandomBytes(16)
		};

		cts.CancelAfter(TimeSpan.FromMilliseconds(50));

		// Act
		await sut.TrackChangesAsync(parameters, cts.Token);

		// Assert
		await dbAccess.DidNotReceive().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test of <see cref="FileChangeTracker.TrackChangesAsync" />.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Encrypts_Contents_When_Session_Dek_Is_Provided()
	{
		// Arrange		
		using CancellationTokenSource cts = new();

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] previousContents = TestUtils.CreateRandomBytes(32);

			byte[] currentContents = TestUtils.CreateRandomBytes(32);

			byte[] encryptedContents = TestUtils.CreateRandomBytes(48);

			byte[] previousHash = TestUtils.CreateRandomBytes(32);

			byte[] currentHash = TestUtils.CreateRandomBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

			fileSystem
				.ComputeSha256HashAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(previousHash, currentHash);

			entityEncryption
				.EncryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(encryptedContents);

			dbAccess
				.UpdateFilePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cts.Cancel();

					return true;
				});

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			Contents = TestUtils.CreateRandomBytes(10),
			File = TestUtils.CreateFileDto(),
			FileName = AppUtils.CreateRandomFileName(10),
			FilePath = AppUtils.CreateRandomFileName(10),
			SessionEncryptedDek = TestUtils.CreateRandomBytes(16)
		};

		// Act
		await sut.TrackChangesAsync(parameters, cts.Token);

		// Assert
		entityEncryption
			.Received(1)
			.EncryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());

		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			parameters.File.Id,
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test of <see cref="FileChangeTracker.TrackChangesAsync" />.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Exits_Gracefully_When_File_Disappears()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = TestUtils.CreateRandomBytes(32);

			byte[] hash = TestUtils.CreateRandomBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(false);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(_ => new MemoryStream(contents));

			fileSystem
				.ComputeSha256HashAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(hash);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			Contents = TestUtils.CreateRandomBytes(10),
			File = TestUtils.CreateFileDto(),
			FileName = AppUtils.CreateRandomFileName(10),
			FilePath = AppUtils.CreateRandomFileName(10),
			SessionEncryptedDek = TestUtils.CreateRandomBytes(16)
		};

		// Act
		Func<Task> act = () => sut.TrackChangesAsync(parameters);

		// Assert
		await act
			.Should()
			.NotThrowAsync();

		await dbAccess.DidNotReceive().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test of <see cref="FileChangeTracker.TrackChangesAsync" />.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Shows_Error_And_Stops_When_Encryption_Fails()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IViewModelExecutionService viewModel = Substitute.For<IViewModelExecutionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] previousContents = TestUtils.CreateRandomBytes(32);

			byte[] currentContents = TestUtils.CreateRandomBytes(32);

			byte[] previousHash = TestUtils.CreateRandomBytes(32);

			byte[] currentHash = TestUtils.CreateRandomBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

			fileSystem
				.ComputeSha256HashAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(previousHash, currentHash);

			IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

			entityEncryption
				.EncryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(default(byte[]));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(viewModel);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			Contents = TestUtils.CreateRandomBytes(10),
			File = TestUtils.CreateFileDto(),
			FileName = AppUtils.CreateRandomFileName(10),
			FilePath = AppUtils.CreateRandomFileName(10),
			SessionEncryptedDek = TestUtils.CreateRandomBytes(16)
		};

		// Act
		await sut.TrackChangesAsync(parameters);

		// Assert
		viewModel
			.Received(1)
			.ExecuteInEditor(Arg.Any<Action<EditorViewModel>>());

		await dbAccess.DidNotReceive().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// Test of <see cref="FileChangeTracker.TrackChangesAsync" />.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Updates_File_When_Contents_Changed()
	{
		// Arrange
		using CancellationTokenSource cts = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] previousContents = TestUtils.CreateRandomBytes(32);

			byte[] currentContents = TestUtils.CreateRandomBytes(32);

			byte[] previousHash = TestUtils.CreateRandomBytes(32);

			byte[] currentHash = TestUtils.CreateRandomBytes(32);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

			fileSystem
				.ComputeSha256HashAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(previousHash, currentHash);

			dbAccess
				.UpdateFilePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cts.Cancel();

					return true;
				});

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			Contents = TestUtils.CreateRandomBytes(10),
			File = TestUtils.CreateFileDto(),
			FileName = AppUtils.CreateRandomFileName(10),
			FilePath = AppUtils.CreateRandomFileName(10),
			SessionEncryptedDek = TestUtils.CreateRandomBytes(16)
		};

		DateTime before = DateTime.Now;

		// Act
		await sut.TrackChangesAsync(parameters, cts.Token);

		// Assert
		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			parameters.File.Id,
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
			Arg.Any<CancellationToken>());

		parameters.File.UpdatedDate
			.Should()
			.BeOnOrAfter(before);
	}
	#endregion
}
