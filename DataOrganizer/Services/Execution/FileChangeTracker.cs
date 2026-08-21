using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Execution;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Messages;
using Repository.Interfaces;
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

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public FileChangeTracker(
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IFileSystem fileSystem,
		ILogger logger,
		IMessenger messenger)
	{
		_dbAccess = dbAccess;

		_contentCipher = contentCipher;

		_fileSystem = fileSystem;

		_logger = logger;

		_messenger = messenger;
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
				if (!await CheckOnceAsync(token).ConfigureAwait(false))
				{
					return;
				}

				await Task
					.Delay(800, token)
					.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			}

			// Tracking is being stopped: persist what changed just before that, while the key is still available.
			await CheckOnceAsync(CancellationToken.None).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// User-initiated cancellation — normal flow, no notification, no log noise.
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			PublishFailure($@"{Strings.FailedToLoadFileContents} ""{parameters.FileName}""");
		}

		void PublishFailure(string message)
		{
			_messenger.ShowSnackbar(message, SnackbarMessageLevel.Error);

			_messenger.Send(new CloseExecutingFileMessage(parameters.File));
		}

		// Compares the file against the previously seen state and persists it when it differs;
		// <c>False</c> asks the caller to stop tracking.
		async Task<bool> CheckOnceAsync(CancellationToken checkToken)
		{
			if (!_fileSystem.IsFileExists(parameters.FilePath))
			{
				PublishFailure($@"{Strings.File} ""{parameters.FileName}"" {Strings.DoesNotExist}");

				return false;
			}

			Stream fileStream;

			try
			{
				fileStream = _fileSystem.OpenRead(parameters.FilePath);
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);

				PublishFailure($@"{Strings.FailedToLoadFileContents} ""{parameters.FileName}""");

				return false;
			}

			byte[] currentHash;

			try
			{
				currentHash = await _fileSystem
					.ComputeStreamHashAsync(TrackChangesParameters.HashAlgorithm, fileStream, checkToken)
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
						.ReadExactlyAsync(bytes, checkToken)
						.ConfigureAwait(false);

					byte[]? cleartext = null;

					try
					{
						if (parameters.KeeperId is { } keeperId)
						{
							byte[] encrypted;

							if (_contentCipher.TryEncrypt(
								keeperId,
								ContentIdentity.ForContents(parameters.File.Id),
								bytes) is not { } ciphertext)
							{
								PublishFailure($@"{Strings.FailedToProcessContents} ""{parameters.FileName}""");

								return false;
							}

							encrypted = ciphertext;

							cleartext = bytes;

							bytes = encrypted;
						}

						DateTime updatedDate = DateTime.Now;

						if (await _dbAccess.UpdateFilePropertiesAsync(parameters.File.Id,
							[
								x => x.SetProperty(x => x.Contents, bytes),
								x => x.SetProperty(x => x.UpdatedDate, updatedDate)
							], checkToken).ConfigureAwait(false))
						{
							_logger.LogDebug(
								"Contents of file is updated in database:" + Environment.NewLine +
								$"File Id = {parameters.File.Id}," + Environment.NewLine +
								$"New bytes length = {bytes.Length}.");

							parameters
								.File
								.UpdatedDate = updatedDate;
						}
					}
					finally
					{
						bytes.ZeroMemory();

						cleartext?.ZeroMemory();
					}
				}

				previousHash = currentHash;
			}
			finally
			{
				fileStream.Dispose();
			}

			return true;
		}
	}
	#endregion
}
