using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IFileChangeTracker" />
public class FileChangeTracker : IFileChangeTracker
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEntityEncryption" />
	private readonly IEntityEncryption _entityEncryption;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public FileChangeTracker(
		IDbAccess dbAccess,
		IEntityEncryption entityEncryption,
		IFileSystem fileSystem,
		ILogger logger,
		IMessenger messenger)
	{
		_dbAccess = dbAccess;

		_entityEncryption = entityEncryption;

		_fileSystem = fileSystem;

		_logger = logger;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task TrackChangesAsync(TrackChangesParameters parameters, CancellationToken token = default)
	{
		try
		{
			HashAlgorithmName algorithm = HashAlgorithmName.SHA256;

			byte[] previousHash = CryptographicOperations.HashData(
				algorithm,
				parameters.Contents);

			while (!token.IsCancellationRequested)
			{
				if (!_fileSystem.IsFileExists(parameters.FilePath))
				{
					PublishFailure($@"{Strings.File} ""{parameters.FileName}"" {Strings.DoesNotExist}");

					return;
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

					return;
				}

				byte[] currentHash;

				try
				{
					currentHash = await _fileSystem
						.ComputeStreamHashAsync(algorithm, fileStream, token)
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
							if (parameters.SessionEncryptedDek is not null)
							{
								if (_entityEncryption.EncryptSessionContents(bytes, parameters.SessionEncryptedDek) is not { } encrypted)
								{
									PublishFailure($@"{Strings.FailedToProcessContents} ""{parameters.FileName}""");

									return;
								}

								cleartext = bytes;

								bytes = encrypted;
							}

							DateTime updatedDate = DateTime.Now;

							if (await _dbAccess.UpdateFilePropertiesAsync(parameters.File.Id,
								[
									x => x.SetProperty(x => x.Contents, bytes),
									x => x.SetProperty(x => x.UpdatedDate, updatedDate)
								], token).ConfigureAwait(false))
							{
								_logger.LogDebug(
									"Contents of file is updated in database:" + Environment.NewLine +
									$"File Id = {parameters.File.Id}," + Environment.NewLine +
									$"File path = {parameters.FilePath}," + Environment.NewLine +
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

				// Polling interval between change checks.
				await Task
					.Delay(800, token)
					.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			}
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
		finally
		{
			parameters
				.SessionEncryptedDek?
				.ZeroMemory();

			parameters
				.Contents
				.ZeroMemory();
		}

		void PublishFailure(string message)
		{
			_messenger.Send(new ShowSnackbarMessage(message, SnackbarMessageLevel.Error));

			_messenger.Send(new CloseExecutingFileMessage(parameters.File));
		}
	}
	#endregion
}
