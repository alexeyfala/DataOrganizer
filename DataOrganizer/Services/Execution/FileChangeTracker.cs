using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Messages.Execution;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Execution;

public class FileChangeTracker : IFileChangeTracker
{
	#region Data
	/// <inheritdoc cref="IContentCipher" />
	private readonly IContentCipher _contentCipher;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FileChangeTracker(
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IDbFailureReporter dbFailureReporter,
		IFileSystem fileSystem,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification)
	{
		_dbAccess = dbAccess;

		_dbFailureReporter = dbFailureReporter;

		_contentCipher = contentCipher;

		_fileSystem = fileSystem;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;
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
					.Delay(800, token)
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

				byte[]? cleartext = null;

				try
				{
					if (parameters.KeeperId is { } keeperId)
					{
						byte[] encrypted;

						if (_contentCipher.TryEncrypt(
							keeperId,
							ContentIdentity.Contents,
							bytes) is not { } ciphertext)
						{
							PublishFailure(parameters, $@"{Strings.FailedToProcessContents} ""{parameters.FileName}""");

							return null;
						}

						encrypted = ciphertext;

						cleartext = bytes;

						bytes = encrypted;
					}

					DateTime updatedAt = DateTime.Now;

					try
					{
						if (await _dbAccess.UpdateFilePropertiesAsync(parameters.File.Id,
							[
								x => x.SetProperty(x => x.Contents, bytes),
								x => x.SetProperty(x => x.UpdatedAt, updatedAt)
							], token).ConfigureAwait(false))
						{
							_logger.LogDebug(
								"Contents of file is updated in database:" + Environment.NewLine +
								$"File Id = {parameters.File.Id}," + Environment.NewLine +
								$"New bytes length = {bytes.Length}.");

							parameters
								.File
								.UpdatedAt = updatedAt;
						}
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

						return null;
					}
				}
				finally
				{
					bytes.ZeroMemory();

					cleartext?.ZeroMemory();
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
		_messenger.Send(new CloseExecutingFileMessage(parameters.File));
	}

	/// <summary>
	/// Shows the failure and closes the file being executed.
	/// </summary>
	private void PublishFailure(TrackChangesParameters parameters, string message)
	{
		_notification.ShowErrorSnackbar(message);

		CloseExecutingFile(parameters);
	}
	#endregion
}
