using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.Services;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
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
	public async Task TrackChangesAsync_Does_Not_Update_Database_When_File_Contents_Are_Same()
	{
		// Arrange
		string filePath = Path.Combine(Path.GetTempPath(), $"FileTracker_NoChange_{Guid.NewGuid():N}.bin");

		byte[] contents = [1, 2, 3, 4, 5];

		File.WriteAllBytes(filePath, contents);

		try
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true, true, false);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			using AutoMock mock = AutoMock.GetLoose(builder =>
			{
				builder.RegisterInstance(fileSystem);

				builder.RegisterInstance(dbAccess);
			});

			FileChangeTracker sut = mock.Create<FileChangeTracker>();

			TrackChangesParameters parameters = CreateParameters(filePath, contents);

			using CancellationTokenSource cts = new();

			cts.CancelAfter(TimeSpan.FromMilliseconds(200));

			// Act
			await sut.TrackChangesAsync(parameters, cts.Token);

			// Assert
			await dbAccess
				.Received(0)
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<PropertyNameValuePair[]>());
		}
		finally
		{
			File.Delete(filePath);
		}
	}

	/// <summary>
	/// Test of <see cref="FileChangeTracker.TrackChangesAsync" />.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Exits_Immediately_When_File_Does_Not_Exist()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		fileSystem
			.IsFileExists(Arg.Any<string>())
			.Returns(false);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = CreateParameters("missing.bin", contents: [1, 2, 3]);

		// Act
		await sut.TrackChangesAsync(parameters);

		// Assert
		await dbAccess
			.Received(0)
			.UpdatePropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>(),
				Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="FileChangeTracker.TrackChangesAsync" />.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Exits_Immediately_When_Token_Is_Already_Cancelled()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		fileSystem
			.IsFileExists(Arg.Any<string>())
			.Returns(true);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(dbAccess);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = CreateParameters("any.bin", contents: [1, 2, 3]);

		using CancellationTokenSource cts = new();

		cts.Cancel();

		// Act
		await sut.TrackChangesAsync(parameters, cts.Token);

		// Assert
		await dbAccess
			.Received(0)
			.UpdatePropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>(),
				Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="FileChangeTracker.TrackChangesAsync" />.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Updates_Database_When_File_Contents_Change()
	{
		// Arrange
		string filePath = Path.Combine(Path.GetTempPath(), $"FileTracker_Update_{Guid.NewGuid():N}.bin");

		byte[] originalContents = [1, 2, 3];

		byte[] newContents = [9, 8, 7, 6];

		File.WriteAllBytes(filePath, newContents);

		try
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			using CancellationTokenSource cts = new();

			dbAccess
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<CancellationToken>(),
					Arg.Any<PropertyNameValuePair[]>())
				.Returns(_ =>
				{
					// Cancel the token after the very first DB update so the loop does not sleep its full delay.
					cts.Cancel();

					return Task.FromResult(true);
				});

			using AutoMock mock = AutoMock.GetLoose(builder =>
			{
				builder.RegisterInstance(fileSystem);

				builder.RegisterInstance(dbAccess);
			});

			FileChangeTracker sut = mock.Create<FileChangeTracker>();

			TrackChangesParameters parameters = CreateParameters(filePath, originalContents);

			// Act
			await sut.TrackChangesAsync(parameters, cts.Token);

			// Assert
			await dbAccess
				.Received()
				.UpdatePropertiesAsync(
					parameters.File.Id,
					Arg.Any<CancellationToken>(),
					Arg.Any<PropertyNameValuePair[]>());

			parameters.Contents
				.Should()
				.BeEquivalentTo(newContents);
		}
		finally
		{
			File.Delete(filePath);
		}
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
		Semaphore = new SemaphoreSlim(1, 1),
		SessionEncryptedDek = null
	};
	#endregion
}
