using DataOrganizer.DTO;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
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

	/// <inheritdoc cref="IViewModelExecutionService" />
	private readonly IViewModelExecutionService _viewModel;
	#endregion

	#region Constructors
	public FileChangeTracker(
		IDbAccess dbAccess,
		IEntityEncryption entityEncryption,
		IFileSystem fileSystem,
		ILogger logger,
		IViewModelExecutionService viewModel)
	{
		_dbAccess = dbAccess;

		_entityEncryption = entityEncryption;

		_fileSystem = fileSystem;

		_logger = logger;

		_viewModel = viewModel;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task TrackChangesAsync(TrackChangesParameters parameters, CancellationToken token = default)
	{
		try
		{
			Stream initial;

			try
			{
				initial = _fileSystem.OpenRead(parameters.FilePath);
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);

				CloseExecutingFile($@"{Strings.FailedToLoadFileContents} ""{parameters.FileName}""");

				return;
			}

			byte[] previousHash;

			try
			{
				previousHash = await _fileSystem
					.ComputeSha256HashAsync(initial, token)
					.ConfigureAwait(false);
			}
			finally
			{
				// Release the stream right after the hash is computed so the file is not held
				// open while the monitoring loop below is running.
				initial.Dispose();
			}

			while (!token.IsCancellationRequested)
			{
				if (!_fileSystem.IsFileExists(parameters.FilePath))
				{
					CloseExecutingFile($@"{Strings.File} ""{parameters.FileName}"" {Strings.DoesNotExist}");

					return;
				}

				Stream currentStream;

				try
				{
					currentStream = _fileSystem.OpenRead(parameters.FilePath);
				}
				catch (Exception ex)
				{
					_logger.LogException(ex);

					CloseExecutingFile($@"{Strings.FailedToLoadFileContents} ""{parameters.FileName}""");

					return;
				}

				byte[] currentHash = previousHash;

				try
				{
					currentHash = await _fileSystem
						.ComputeSha256HashAsync(currentStream, token)
						.ConfigureAwait(false);

					if (!currentHash.SequenceEqual(previousHash))
					{
						currentStream.Position = 0;

						// 'checked' guards against silently truncating files larger than
						// int.MaxValue (~2 GB). For text / editor files this branch is
						// effectively unreachable, but if it ever is, we want a clear
						// OverflowException instead of a corrupted partial read.
						int length = checked((int)currentStream.Length);

						byte[] bytes = new byte[length];

						await currentStream
							.ReadExactlyAsync(bytes, token)
							.ConfigureAwait(false);

						byte[]? cleartext = null;

						try
						{
							if (parameters.SessionEncryptedDek is not null)
							{
								if (_entityEncryption.EncryptSessionContents(bytes, parameters.SessionEncryptedDek) is not { } encrypted)
								{
									CloseExecutingFile($@"{Strings.FailedToProcessContents} ""{parameters.FileName}""");

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
				}
				finally
				{
					previousHash = currentHash;

					currentStream.Dispose();
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

			CloseExecutingFile($@"{Strings.FailedToLoadFileContents} ""{parameters.FileName}""");
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

		void CloseExecutingFile(string message)
		{
			_viewModel.ExecuteInBaseViewModel(x => x.ShowErrorSnackbar(message));

			_viewModel.ExecuteInBaseViewModel(x => x.CloseExecutingFile(parameters.File));
		}
	}
	#endregion
}
