using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Diagnostics.Interfaces;
using FileSystemOperations.Services;
using Notifications.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Shared.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Database;

namespace FileSystemOperations.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(BackedUpWriter)}"" type")]
internal class BackedUpWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="BackedUpWriter.WriteAsync" />: a done write keeps the database as it wrote it.
	/// </summary>
	[Test]
	public async Task WriteAsync_Keeps_A_Done_Write()
	{
		// Arrange
		IDbBackupService backupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			backupService
				.CreateBackupAsync(Arg.Any<CancellationToken>())
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(backupService);
		});

		BackedUpWriter sut = mock.Create<BackedUpWriter>();

		// Act
		bool result = await sut.WriteAsync(() => Task.FromResult(true));

		// Assert
		result
			.Should()
			.BeTrue();

		await backupService
			.DidNotReceiveWithAnyArgs()
			.RestoreFromBackupAsync(default!, default);
	}

	/// <summary>
	/// <see cref="BackedUpWriter.WriteAsync" />: a rollback that fails goes to the reporter of database failures.
	/// </summary>
	[Test]
	public async Task WriteAsync_Reports_A_Failed_Rollback()
	{
		// Arrange
		InvalidOperationException failure = new();

		IDbFailureReporter failureReporter = Substitute.For<IDbFailureReporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbBackupService backupService = Substitute.For<IDbBackupService>();

			backupService
				.CreateBackupAsync(Arg.Any<CancellationToken>())
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			backupService
				.RestoreFromBackupAsync(default!, default)
				.ThrowsAsyncForAnyArgs(failure);

			builder.RegisterInstance(backupService);

			builder.RegisterInstance(failureReporter);
		});

		BackedUpWriter sut = mock.Create<BackedUpWriter>();

		// Act
		bool result = await sut.WriteAsync(() => Task.FromResult(false));

		// Assert
		result
			.Should()
			.BeFalse();

		failureReporter
			.Received(1)
			.Report(failure, Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="BackedUpWriter.WriteAsync" />: a write that answers <c>false</c> or throws brings the database back to the
	/// copy without the token of the caller, which is cancelled, and the exception goes on.
	/// </summary>
	[Test]
	public async Task WriteAsync_Rolls_Back_A_Failed_Write([Values] bool doesWriteThrow)
	{
		// Arrange
		DatabaseBackup backup = DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>());

		IDbBackupService backupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			backupService
				.CreateBackupAsync(Arg.Any<CancellationToken>())
				.Returns(backup);

			builder.RegisterInstance(backupService);
		});

		BackedUpWriter sut = mock.Create<BackedUpWriter>();

		using CancellationTokenSource cancellation = new();

		await cancellation.CancelAsync();

		// Act
		Func<Task<bool>> act = () => sut.WriteAsync(
			() => doesWriteThrow
				? throw new InvalidOperationException()
				: Task.FromResult(false),
			cancellation.Token);

		// Assert
		if (doesWriteThrow)
		{
			await act
				.Should()
				.ThrowAsync<InvalidOperationException>();
		}
		else
		{
			(await act())
				.Should()
				.BeFalse();
		}

		await backupService
			.Received(1)
			.RestoreFromBackupAsync(backup.FilePath, CancellationToken.None);
	}

	/// <summary>
	/// <see cref="BackedUpWriter.WriteAsync" />: without a copy of the database the write does not run, the answer is
	/// <c>false</c>, and an error is shown, whether the copy failed or came out empty.
	/// </summary>
	[Test]
	public async Task WriteAsync_Runs_No_Write_Without_A_Copy([Values] bool doesCopyThrow)
	{
		// Arrange
		bool isRun = false;

		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbBackupService backupService = Substitute.For<IDbBackupService>();

			if (doesCopyThrow)
			{
				backupService
					.CreateBackupAsync(Arg.Any<CancellationToken>())
					.ThrowsAsync(new InvalidOperationException());
			}
			else
			{
				// No copy could be made.
				backupService
					.CreateBackupAsync(Arg.Any<CancellationToken>())
					.Returns((DatabaseBackup?)null);
			}

			builder.RegisterInstance(backupService);

			builder.RegisterInstance(notification);
		});

		BackedUpWriter sut = mock.Create<BackedUpWriter>();

		// Act
		bool result = await sut.WriteAsync(() =>
		{
			isRun = true;

			return Task.FromResult(true);
		});

		// Assert
		result
			.Should()
			.BeFalse();

		isRun
			.Should()
			.BeFalse();

		notification
			.Received(1)
			.ShowErrorSnackbar(Arg.Any<string>(), Arg.Any<string>());
	}
	#endregion
}
