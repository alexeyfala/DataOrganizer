using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Database.Repository.Enums;
using DataOrganizer.Database.Repository.Exceptions;
using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Documents.FileContents.Dto;
using DataOrganizer.Documents.FileContents.Enums;
using DataOrganizer.Documents.FileContents.Interfaces;
using DataOrganizer.Documents.FileContents.Services;
using DataOrganizer.Hierarchy.UnitTests.Factories;
using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Infrastructure.Notifications.Interfaces;
using DataOrganizer.Security.Helpers;
using DataOrganizer.Security.Interfaces;
using DataOrganizer.SystemIntegration.FileExecution.Dto;
using DataOrganizer.SystemIntegration.FileExecution.Messages;
using DataOrganizer.SystemIntegration.FileExecution.Services;
using DataOrganizer.TestSupport.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Testably.Abstractions.Testing;
using TimeProvider = System.TimeProvider;

namespace DataOrganizer.SystemIntegration.UnitTests.FileExecution.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FileChangeTracker)}"" type")]
internal class FileChangeTrackerTests
{
	#region Data
	/// <summary>
	/// Mirrors the pause between two checks of the file.
	/// </summary>
	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(800.0);

	/// <summary>
	/// Path of the tracked file.
	/// </summary>
	private static readonly string TrackedFilePath = TestPath.Combine("tracked.txt");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: after a pause the file is checked again, and a change seen then is
	/// saved.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Checks_The_File_Again_After_A_Pause()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		FakeTimeProvider time = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		byte[] previousContents = RandomValues.CreateBytes(32);

		byte[] previousHash = CryptographicOperations.HashData(TrackChangesParameters.HashAlgorithm, previousContents);

		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(TrackedFilePath, previousContents);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return true;
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = previousHash,
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		Task tracking = sut.TrackChangesAsync(parameters, cancellation.Token);

		// The first check sees the file as it was; the change comes while the tracking waits.
		files
			.File
			.WriteAllBytes(TrackedFilePath, RandomValues.CreateBytes(32));

		await Task.Run(() => time.Advance(PollInterval - TimeSpan.FromTicks(1)));

		// Act
		await Task.Run(() => time.Advance(TimeSpan.FromTicks(1)));

		// Assert
		tracking.IsCompleted
			.Should()
			.BeTrue();

		await tracking;

		await dbAccess.Received(1).UpdateFileContentsAsync(
			parameters.File.Id,
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>(),
			Arg.Any<CancellationToken>());

		// The change is stamped with the moment the pause ended, so the check came neither earlier nor later.
		parameters.File.UpdatedAt
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a file that stays locked through the checks that may miss it is
	/// closed, and tracking stops.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Closes_The_File_That_Stays_Locked()
	{
		// Arrange
		FakeTimeProvider time = new();

		INotificationService notification = Substitute.For<INotificationService>();

		StrongReferenceMessenger messenger = new();

		Guid? receivedClosedId = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedId = message.FileId);

		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(TrackedFilePath, RandomValues.CreateBytes(32));

		// Another program keeps the file to itself.
		using FileSystemStream holder = files
			.File
			.Open(
				TrackedFilePath,
				FileMode.Open,
				FileAccess.ReadWrite,
				FileShare.None);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(notification);

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		Task tracking = sut.TrackChangesAsync(parameters);

		await Task.Run(() => time.Advance(PollInterval));

		await Task.Run(() => time.Advance(PollInterval));

		Guid? closedIdBeforeLastCheck = receivedClosedId;

		// Act
		await Task.Run(() => time.Advance(PollInterval));

		// Assert
		// The first check and the two after it may miss the file, so only the third pause ends the tracking.
		closedIdBeforeLastCheck
			.Should()
			.BeNull();

		tracking.IsCompleted
			.Should()
			.BeTrue();

		await tracking;

		receivedClosedId
			.Should()
			.Be(parameters.File.Id);

		// The failure is shown once, and nothing runs after it.
		notification
			.Received(1)
			.ShowErrorSnackbar(Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a file that stays missing through the checks that may miss it is
	/// closed without an update, and tracking stops.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Closes_The_File_That_Stays_Missing()
	{
		// Arrange
		FakeTimeProvider time = new();

		IFileContentsWriter fileContentsWriter = Substitute.For<IFileContentsWriter>();

		StrongReferenceMessenger messenger = new();

		Guid? receivedClosedId = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedId = message.FileId);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder.RegisterInstance(fileContentsWriter);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		Task tracking = sut.TrackChangesAsync(parameters);

		await Task.Run(() => time.Advance(PollInterval));

		await Task.Run(() => time.Advance(PollInterval));

		Guid? closedIdBeforeLastCheck = receivedClosedId;

		// Act
		await Task.Run(() => time.Advance(PollInterval));

		// Assert
		// The first check and the two after it may miss the file, so only the third pause ends the tracking.
		closedIdBeforeLastCheck
			.Should()
			.BeNull();

		tracking.IsCompleted
			.Should()
			.BeTrue();

		await tracking;

		receivedClosedId
			.Should()
			.Be(parameters.File.Id);

		await fileContentsWriter.DidNotReceive().WriteAsync(
			Arg.Any<Guid>(),
			Arg.Any<Guid?>(),
			Arg.Any<byte[]>(),
			Arg.Any<FileContentsChange>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a check that fails closes the file, and tracking stops.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Closes_The_File_When_A_Check_Fails()
	{
		// Arrange
		StrongReferenceMessenger messenger = new();

		Guid? receivedClosedId = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedId = message.FileId);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			MockFileSystem streams = new();

			streams
				.File
				.WriteAllBytes(TrackedFilePath, RandomValues.CreateBytes(32));

			// The file is there, under its name in the exact case.
			fileSystem
				.File
				.Exists(TrackedFilePath)
				.Returns(true);

			fileSystem
				.Directory
				.EnumerateFiles(TestPath.Root)
				.Returns([TrackedFilePath]);

			// The mock cannot fail a read, so the check gets a stream that cannot be read at all.
			fileSystem
				.File
				.Open(
					TrackedFilePath,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete)
				.Returns(_ => streams.File.Open(TrackedFilePath, FileMode.Open, FileAccess.Write));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Act
		Task tracking = sut.TrackChangesAsync(parameters);

		// Assert
		// The tracking stops at once rather than waiting for the next check.
		tracking.IsCompleted
			.Should()
			.BeTrue();

		await tracking;

		receivedClosedId
			.Should()
			.Be(parameters.File.Id);
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: the file is read through a stream that is closed after the check,
	/// so the program that runs the file can save it again.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Closes_The_Stream_After_A_Check()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(32);

		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(TrackedFilePath, contents);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = CryptographicOperations.HashData(TrackChangesParameters.HashAlgorithm, contents),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Act
		// A stopped tracking makes only the last check.
		await sut.TrackChangesAsync(parameters, new CancellationToken(canceled: true));

		// Assert
		// The file can be taken alone, so no stream of the check holds it any more.
		Action act = () => files
			.File
			.Open(
				TrackedFilePath,
				FileMode.Open,
				FileAccess.ReadWrite,
				FileShare.None)
			.Dispose();

		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a file that is back counts its missed checks anew, so a later
	/// moment without it does not close it.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Counts_The_Missed_Checks_Anew_After_The_File_Is_Back()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		FakeTimeProvider time = new();

		byte[] contents = RandomValues.CreateBytes(32);

		StrongReferenceMessenger messenger = new();

		Guid? receivedClosedId = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedId = message.FileId);

		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = CryptographicOperations.HashData(TrackChangesParameters.HashAlgorithm, contents),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Three checks miss the file, one finds it, the next misses it again, and the last check after the stop finds it.
		Task tracking = sut.TrackChangesAsync(parameters, cancellation.Token);

		await Task.Run(() => time.Advance(PollInterval));

		await Task.Run(() => time.Advance(PollInterval));

		files
			.File
			.WriteAllBytes(TrackedFilePath, contents);

		await Task.Run(() => time.Advance(PollInterval));

		files
			.File
			.Delete(TrackedFilePath);

		// Act
		await Task.Run(() => time.Advance(PollInterval));

		// Assert
		tracking.IsCompleted
			.Should()
			.BeFalse();

		receivedClosedId
			.Should()
			.BeNull();

		files
			.File
			.WriteAllBytes(TrackedFilePath, contents);

		await cancellation.CancelAsync();

		await tracking;
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: the file is not updated when the content hash is unchanged.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Does_Not_Update_When_Contents_Did_Not_Change()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		byte[] contents = RandomValues.CreateBytes(32);

		byte[] expectedHash = CryptographicOperations.HashData(TrackChangesParameters.HashAlgorithm, contents);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, contents);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = expectedHash,
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		await dbAccess.DidNotReceive().UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>(),
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
			byte[] currentContents = RandomValues.CreateBytes(32);

			byte[] encryptedContents = RandomValues.CreateBytes(48);

			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, currentContents);

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(encryptedContents);

			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return true;
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath,
			KeeperId = Guid.NewGuid()
		};

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		contentCipher
			.Received(1)
			.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());

		await dbAccess.Received(1).UpdateFileContentsAsync(
			parameters.File.Id,
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: tracking goes on after a saved change, so a later change is
	/// saved too.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Goes_On_After_A_Saved_Change()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		FakeTimeProvider time = new();

		IFileContentsWriter fileContentsWriter = Substitute.For<IFileContentsWriter>();

		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(TrackedFilePath, RandomValues.CreateBytes(32));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			int writes = 0;

			fileContentsWriter
				.WriteAsync(
					Arg.Any<Guid>(),
					Arg.Any<Guid?>(),
					Arg.Any<byte[]>(),
					Arg.Any<FileContentsChange>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					if (++writes == 2)
					{
						cancellation.Cancel();
					}

					return new FileContentsWriting
					{
						Status = FileContentsWriteStatus.Written
					};
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(fileContentsWriter);

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Each check sees another change, and the last check after the stop sees none.
		Task tracking = sut.TrackChangesAsync(parameters, cancellation.Token);

		files
			.File
			.WriteAllBytes(TrackedFilePath, RandomValues.CreateBytes(32));

		// Act
		await Task.Run(() => time.Advance(PollInterval));

		// Assert
		tracking.IsCompleted
			.Should()
			.BeTrue();

		await tracking;

		await fileContentsWriter.Received(2).WriteAsync(
			Arg.Any<Guid>(),
			Arg.Any<Guid?>(),
			Arg.Any<byte[]>(),
			Arg.Any<FileContentsChange>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a change that finds no file in the database leaves its date of
	/// change alone and keeps it open.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Keeps_The_Date_When_The_File_Is_Not_In_The_Database()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		StrongReferenceMessenger messenger = new();

		Guid? receivedClosedId = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedId = message.FileId);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] currentContents = RandomValues.CreateBytes(32);

			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, currentContents);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			// No row takes the change, as the file is gone from the database.
			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return false;
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		DateTime updatedAt = parameters.File.UpdatedAt;

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		parameters.File.UpdatedAt
			.Should()
			.Be(updatedAt);

		receivedClosedId
			.Should()
			.BeNull();
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
			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, RandomValues.CreateBytes(32));

			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		await dbAccess.Received(1).UpdateFileContentsAsync(
			parameters.File.Id,
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a change is read while the program the file is open in keeps it
	/// open for writing.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Reads_The_File_While_Its_Program_Keeps_It_Open()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(TrackedFilePath, RandomValues.CreateBytes(32));

		using FileSystemStream program = files
			.File
			.Open(
				TrackedFilePath,
				FileMode.Open,
				FileAccess.ReadWrite,
				FileShare.ReadWrite);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return true;
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance<TimeProvider>(new FakeTimeProvider());
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		await dbAccess.Received(1).UpdateFileContentsAsync(
			parameters.File.Id,
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a failed save is reported, the file is closed and tracking stops.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Reports_A_Failed_Save_And_Stops()
	{
		// Arrange
		IDbFailureReporter dbFailureReporter = Substitute.For<IDbFailureReporter>();

		StrongReferenceMessenger messenger = new();

		Guid? receivedClosedId = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedId = message.FileId);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] currentContents = RandomValues.CreateBytes(32);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>(),
					Arg.Any<CancellationToken>())
				.ThrowsAsync(new DatabaseNotWritableException(DbConnectionStatus.FileUnreadable, nameof(IDbAccess)));

			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, currentContents);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance(dbFailureReporter);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Act
		Task tracking = sut.TrackChangesAsync(parameters);

		// Assert
		// The tracking stops at once rather than waiting for the next check.
		tracking.IsCompleted
			.Should()
			.BeTrue();

		await tracking;

		dbFailureReporter.Received(1).Report(
			Arg.Any<DatabaseNotWritableException>(),
			Arg.Any<string>());

		receivedClosedId
			.Should()
			.Be(parameters.File.Id);
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: the last check after the stop may not miss the file, so a file
	/// missing then is reported.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Reports_A_File_Missing_At_The_Stop()
	{
		// Arrange
		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder.RegisterInstance(notification);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Act
		// A stopped tracking makes only the last check.
		await sut.TrackChangesAsync(parameters, new CancellationToken(canceled: true));

		// Assert
		notification
			.Received(1)
			.ShowErrorSnackbar(Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a file missing for the checks that may miss it, as while a
	/// program saves it through a temporary file, stays open, and its change is saved once it is back.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Saves_A_Change_After_The_File_Comes_Back()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		FakeTimeProvider time = new();

		IFileContentsWriter fileContentsWriter = Substitute.For<IFileContentsWriter>();

		StrongReferenceMessenger messenger = new();

		Guid? receivedClosedId = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedId = message.FileId);

		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			fileContentsWriter
				.WriteAsync(
					Arg.Any<Guid>(),
					Arg.Any<Guid?>(),
					Arg.Any<byte[]>(),
					Arg.Any<FileContentsChange>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return new FileContentsWriting
					{
						Status = FileContentsWriteStatus.Written
					};
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(fileContentsWriter);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		Task tracking = sut.TrackChangesAsync(parameters, cancellation.Token);

		await Task.Run(() => time.Advance(PollInterval));

		await Task.Run(() => time.Advance(PollInterval));

		// The file comes back, changed, on the check that would otherwise close it.
		files
			.File
			.WriteAllBytes(TrackedFilePath, RandomValues.CreateBytes(32));

		// Act
		await Task.Run(() => time.Advance(PollInterval));

		// Assert
		tracking.IsCompleted
			.Should()
			.BeTrue();

		await tracking;

		await fileContentsWriter.Received(1).WriteAsync(
			parameters.File.Id,
			Arg.Any<Guid?>(),
			Arg.Any<byte[]>(),
			Arg.Any<FileContentsChange>(),
			Arg.Any<CancellationToken>());

		receivedClosedId
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: a file emptied under a keeper is saved without encryption, as empty
	/// content always is.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Saves_Emptied_Contents_Unencrypted_Under_A_Keeper()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, []);

			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return true;
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath,
			KeeperId = Guid.NewGuid()
		};

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		contentCipher
			.DidNotReceiveWithAnyArgs()
			.TryEncrypt(default, default, default!);

		await dbAccess.Received(1).UpdateFileContentsAsync(
			parameters.File.Id,
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>(),
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

		Guid? receivedClosedId = null;

		object recipient = new();

		messenger.Register<CloseExecutingFileMessage>(recipient, (_, message) => receivedClosedId = message.FileId);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] currentContents = RandomValues.CreateBytes(32);

			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, currentContents);

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The cipher swallows the cryptographic failure and answers with a refusal.

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath,
			KeeperId = Guid.NewGuid()
		};

		// Act
		Task tracking = sut.TrackChangesAsync(parameters);

		// Assert
		// The tracking stops at once rather than waiting for the next check.
		tracking.IsCompleted
			.Should()
			.BeTrue();

		await tracking;

		receivedClosedId
			.Should()
			.Be(parameters.File.Id);

		await dbAccess.DidNotReceive().UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>(),
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

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] currentContents = RandomValues.CreateBytes(32);

			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, currentContents);

			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>(),
					Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					cancellation.Cancel();

					return true;
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Act
		await sut.TrackChangesAsync(parameters, cancellation.Token);

		// Assert
		await dbAccess.Received(1).UpdateFileContentsAsync(
			parameters.File.Id,
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>(),
			Arg.Any<CancellationToken>());

		parameters.File.UpdatedAt
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);
	}

	/// <summary>
	/// <see cref="FileChangeTracker.TrackChangesAsync" />: the changed bytes of the file are written as they are, and the
	/// copy read for them is wiped afterwards.
	/// </summary>
	[Test]
	public async Task TrackChangesAsync_Writes_The_Changed_Contents_And_Wipes_Them()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(32);

		byte[]? written = null;

		byte[]? passed = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem files = new();

			files
				.File
				.WriteAllBytes(TrackedFilePath, contents);

			IFileContentsWriter fileContentsWriter = Substitute.For<IFileContentsWriter>();

			fileContentsWriter
				.WriteAsync(
					Arg.Any<Guid>(),
					Arg.Any<Guid?>(),
					Arg.Do<byte[]>(x =>
					{
						written = [.. x];

						passed = x;
					}),
					Arg.Any<FileContentsChange>(),
					Arg.Any<CancellationToken>())
				.Returns(new FileContentsWriting
				{
					Status = FileContentsWriteStatus.Written
				});

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(fileContentsWriter);
		});

		FileChangeTracker sut = mock.Create<FileChangeTracker>();

		TrackChangesParameters parameters = new()
		{
			PreviousHash = RandomValues.CreateBytes(32),
			File = ItemDtoFactory.CreateFileDto(),
			FileName = RandomValues.CreateFileName(10),
			FilePath = TrackedFilePath
		};

		// Act
		// A stopped tracking makes only the last check.
		await sut.TrackChangesAsync(parameters, new CancellationToken(canceled: true));

		// Assert
		written
			.Should()
			.Equal(contents);

		passed
			.Should()
			.AllSatisfy(x => x.Should().Be(0));
	}
	#endregion
}
