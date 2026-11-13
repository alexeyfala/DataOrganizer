using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Documents.FileContents.Dto;
using DataOrganizer.Documents.FileContents.Enums;
using DataOrganizer.Documents.FileContents.Interfaces;
using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Infrastructure.Notifications.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Properties;
using DataOrganizer.SystemIntegration.FileExecution.Dto;
using DataOrganizer.SystemIntegration.FileExecution.Interfaces;
using DataOrganizer.SystemIntegration.FileExecution.Messages;
using Serilog;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.FileExecution.Services;

/// <inheritdoc cref="IFileChangeTracker" />
public class FileChangeTracker : IFileChangeTracker
{
	#region Data
	/// <summary>
	/// Checks in a row that may find the file missing or locked before tracking gives up: a program that saves through a
	/// temporary file hides its file for a moment.
	/// </summary>
	private const int MaxMissedChecks = 3;

	/// <summary>
	/// Pause between two checks of the file.
	/// </summary>
	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(800.0);

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IFileContentsWriter" />
	private readonly IFileContentsWriter _fileContentsWriter;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public FileChangeTracker(
		IDbFailureReporter dbFailureReporter,
		IFileContentsWriter fileContentsWriter,
		IFileSystem fileSystem,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		TimeProvider timeProvider)
	{
		_dbFailureReporter = dbFailureReporter;

		_fileContentsWriter = fileContentsWriter;

		_fileSystem = fileSystem;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task TrackChangesAsync(TrackChangesParameters parameters, CancellationToken token = default)
	{
		byte[] previousHash = parameters.PreviousHash;

		int missedCount = 0;

		try
		{
			while (!token.IsCancellationRequested)
			{
				byte[]? currentHash = await CheckOnceAsync(
					parameters,
					previousHash,
					missedCount < MaxMissedChecks,
					token).ConfigureAwait(false);

				if (currentHash is null)
				{
					return;
				}

				// An empty hash marks a check that missed the file.
				if (currentHash.Length == 0)
				{
					missedCount++;
				}
				else
				{
					missedCount = 0;

					previousHash = currentHash;
				}

				await Task
					.Delay(PollInterval, _timeProvider, token)
					.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			}

			// Tracking is being stopped: persist what changed just before that, while the key is still available.
			await CheckOnceAsync(
				parameters,
				previousHash,
				mayMiss: false,
				CancellationToken.None).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// User-initiated cancellation — normal flow, no notification, no log noise.
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			PublishFailure(parameters, $@"{Strings.FailedToLoadFileContents} ""{parameters.FileName}""");
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Compares the file against the previously seen state and persists it when it differs; answers with the hash just
	/// seen, an empty hash when <paramref name="mayMiss" /> lets a missing or locked file pass, or <c>null</c> when
	/// tracking is to stop.
	/// </summary>
	private async Task<byte[]?> CheckOnceAsync(
		TrackChangesParameters parameters,
		byte[] previousHash,
		bool mayMiss,
		CancellationToken token)
	{
		if (!_fileSystem
			.File
			.Exists(parameters.FilePath))
		{
			// Stryker disable once String : wording of a message
			return MissFile(parameters, mayMiss, $@"{Strings.File} ""{parameters.FileName}"" {Strings.DoesNotExist}");
		}

		Stream fileStream;

		try
		{
			// Shared for writing and deleting, so the program the file is open in may change it while it is observed.
			fileStream = _fileSystem
				.File
				.Open(
					parameters.FilePath,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete);
		}
		catch (Exception ex)
		{
			_logger.LogWarning($@"File ""{parameters.FileName}"" cannot be opened: {ex.Message}");

			// Stryker disable once String : wording of a message
			return MissFile(parameters, mayMiss, $@"{Strings.FailedToLoadFileContents} ""{parameters.FileName}""");
		}

		byte[] currentHash;

		try
		{
			currentHash = await CryptographicOperations
				.HashDataAsync(TrackChangesParameters.HashAlgorithm, fileStream, token)
				.ConfigureAwait(false);

			if (!currentHash.SequenceEqual(previousHash))
			{
				fileStream.Position = 0;

				// 'checked' guards against silently truncating files larger than
				// int.MaxValue (~2 GB). For text / editor files this branch is
				// effectively unreachable, but if it ever is, we want a clear
				// OverflowException instead of a corrupted partial read.
				// Stryker disable once Checked : only a file over 2 GB could show it
				int length = checked((int)fileStream.Length);

				byte[] bytes = new byte[length];

				await fileStream
					.ReadExactlyAsync(bytes, token)
					.ConfigureAwait(false);

				try
				{
					if (!await WriteContentsAsync(parameters, bytes, token).ConfigureAwait(false))
					{
						return null;
					}
				}
				finally
				{
					bytes.ZeroMemory();
				}
			}
		}
		finally
		{
			fileStream.Dispose();
		}

		return currentHash;
	}

	/// <summary>
	/// Asks for the file being executed to be closed.
	/// </summary>
	private void CloseExecutingFile(TrackChangesParameters parameters)
	{
		_messenger.Send(new CloseExecutingFileMessage(parameters.File.Id));
	}

	/// <summary>
	/// Answers a check that missed the file: an empty hash when <paramref name="mayMiss" /> lets it pass, otherwise shows
	/// <paramref name="message" />, closes the file and answers <c>null</c>.
	/// </summary>
	private byte[]? MissFile(TrackChangesParameters parameters, bool mayMiss, string message)
	{
		if (!mayMiss)
		{
			PublishFailure(parameters, message);

			return null;
		}

		_logger.LogWarning($@"File ""{parameters.FileName}"" is missing or locked, so it is checked again later");

		return [];
	}

	/// <summary>
	/// Shows the failure and closes the file being executed.
	/// </summary>
	private void PublishFailure(TrackChangesParameters parameters, string message)
	{
		_notification.ShowErrorSnackbar(message);

		CloseExecutingFile(parameters);
	}

	/// <summary>
	/// Writes the changed contents of the file and gives the file its new date of change; <c>false</c> when tracking is
	/// to stop.
	/// </summary>
	private async Task<bool> WriteContentsAsync(
		TrackChangesParameters parameters,
		byte[] contents,
		CancellationToken token)
	{
		FileContentsWriting writing;

		try
		{
			writing = await _fileContentsWriter
				.WriteAsync(parameters.File.Id, parameters.KeeperId, contents, FileContentsChange.Edit, token)
				.ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// Cancellation stops the tracking on its own terms, so it is left to the outer handler.
			throw;
		}
		catch (Exception ex)
		{
			// The changes have nowhere to go, so tracking them further would only lose more of them.
			_dbFailureReporter.Report(
				ex,
				$@"{Strings.FailedToSaveFileContents} ""{parameters.FileName}""");

			CloseExecutingFile(parameters);

			return false;
		}

		if (writing.Status == FileContentsWriteStatus.Closed)
		{
			PublishFailure(parameters, $@"{Strings.FailedToProcessContents} ""{parameters.FileName}""");

			return false;
		}

		if (writing.UpdatedAt is { } updatedAt)
		{
			parameters
				.File
				.UpdatedAt = updatedAt;
		}

		return true;
	}
	#endregion
}
