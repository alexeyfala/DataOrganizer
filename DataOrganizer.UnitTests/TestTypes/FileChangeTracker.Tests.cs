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

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(_ => new MemoryStream(contents));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = CreateParameters(
			AppUtils.CreateRandomFileName(10),
			TestUtils.CreateRandomBytes(10));

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

		IEntityEcryption entityEcryption = Substitute.For<IEntityEcryption>();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] previousContents = TestUtils.CreateRandomBytes(32);

			byte[] currentContents = TestUtils.CreateRandomBytes(32);

			byte[] encryptedContents = TestUtils.CreateRandomBytes(48);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

			entityEcryption
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

			builder.RegisterInstance(entityEcryption);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			Contents = TestUtils.CreateRandomBytes(10),
			File = TestUtils.CreateFileDto(),
			FilePath = AppUtils.CreateRandomFileName(10),
			SessionEncryptedDek = TestUtils.CreateRandomBytes(16)
		};

		// Act
		await sut.TrackChangesAsync(parameters, cts.Token);

		// Assert
		entityEcryption
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

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(false);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(_ => new MemoryStream(contents));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = CreateParameters(
			AppUtils.CreateRandomFileName(10),
			TestUtils.CreateRandomBytes(10));

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

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

			IEntityEcryption entityEcryption = Substitute.For<IEntityEcryption>();

			entityEcryption
				.EncryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns(default(byte[]));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(entityEcryption);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(viewModel);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			Contents = TestUtils.CreateRandomBytes(10),
			File = TestUtils.CreateFileDto(),
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

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(
					_ => new MemoryStream(previousContents),
					_ => new MemoryStream(currentContents));

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

		TrackChangesParameters parameters = CreateParameters(
			AppUtils.CreateRandomFileName(10),
			TestUtils.CreateRandomBytes(10));

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

	#region Service
	/// <summary>
	/// Builds <see cref="TrackChangesParameters" /> for tests with a fresh semaphore and the supplied contents.
	/// </summary>
	private static TrackChangesParameters CreateParameters(string filePath, byte[] contents) => new()
	{
		Contents = contents,
		File = TestUtils.CreateFileDto(),
		FilePath = filePath,
		SessionEncryptedDek = null
	};
	#endregion
}
