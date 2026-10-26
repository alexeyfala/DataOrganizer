using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Messages.Execution;
using Diagnostics.Interfaces;
using Notifications.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TextProcessing.Extensions;

namespace DataOrganizer.Services.Execution;

/// <inheritdoc cref="IFileChangeTracker" />
public class FileChangeTracker : IFileChangeTracker
{
	#region Data
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

		try
		{
			while (!token.IsCancellationRequested)
			{
				if (await CheckOnceAsync(parameters, previousHash, token).ConfigureAwait(false) is not { } currentHash)
				{
					return;
				}

				previousHash = currentHash;

				await Task
					.Delay(PollInterval, _timeProvider, token)
					.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			}

			// Tracking is being stopped: persist what changed just before that, while the key is still available.
			await CheckOnceAsync(parameters, previousHash, CancellationToken.None).ConfigureAwait(false);
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
	/// Compares the file against the previously seen state and persists it when it differs;
	/// answers with the hash just seen, or <c>null</c> when tracking is to stop.
	/// </summary>
	private async Task<byte[]?> CheckOnceAsync(
		TrackChangesParameters parameters,
		byte[] previousHash,
		CancellationToken token)
	{
		if (!_fileSystem.FileExists(parameters.FilePath))
		{
			PublishFailure(parameters, $@"{Strings.File} ""{parameters.FileName}"" {Strings.DoesNotExist}");

			return null;
		}

		Stream fileStream;

		try
		{
			fileStream = _fileSystem.OpenRead(parameters.FilePath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			PublishFailure(parameters, $@"{Strings.FailedToLoadFileContents} ""{parameters.FileName}""");

			return null;
		}

		byte[] currentHash;

		try
		{
			currentHash = await _fileSystem
				.ComputeStreamHashAsync(TrackChangesParameters.HashAlgorithm, fileStream, token)
				.ConfigureAwait(false);

			if (!currentHash.SequenceEqual(previousHash))
			{
				fileStream.Position = 0;

				// 'checked' guards against silently truncating files larger than
				// int.MaxValue (~2 GB). For text / editor files this branch is
				// effectively unreachable, but if it ever is, we want a clear
				// OverflowException instead of a corrupted partial read.
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
