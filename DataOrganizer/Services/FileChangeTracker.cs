using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using Entities.Models;
using Repository.DTO;
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

	/// <inheritdoc cref="IEntityEcryption" />
	private readonly IEntityEcryption _entityEcryption;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public FileChangeTracker(
		IDbAccess dbAccess,
		IEntityEcryption entityEcryption,
		IFileSystem fileSystem,
		ILogger logger)
	{
		_dbAccess = dbAccess;

		_entityEcryption = entityEcryption;

		_fileSystem = fileSystem;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Tracks changes of the executed file.
	/// </summary>
	public async Task TrackChangesAsync(TrackChangesParameters parameters, CancellationToken token = default)
	{
		try
		{
			while (_fileSystem.IsFileExists(parameters.FilePath) && parameters.Condition(parameters.File.Id))
			{
				try
				{
					await parameters
						.Semaphore
						.WaitAsync(token)
						.ConfigureAwait(false);

					if (!_fileSystem.IsFileExists(parameters.FilePath) || !parameters.Condition(parameters.File.Id))
					{
						return;
					}

					await using FileStream fileStream = File.Open(
						parameters.FilePath,
						FileMode.Open,
						FileAccess.Read,
						FileShare.ReadWrite);

					await using MemoryStream memoryStream = new();

					fileStream.CopyTo(memoryStream);

					byte[] bytes = memoryStream.ToArray();

					try
					{
						if (!bytes.SequenceEqual(parameters.Contents))
						{
							byte[] contents = bytes;

							if (parameters.SessionEncryptedDek is not null && !_entityEcryption.Encrypt(
								bytes,
								parameters.SessionEncryptedDek,
								out contents))
							{
								parameters
									.ViewModel?
									.ShowErrorSnackbar(Strings.FailedToProcessContents);

								return;
							}

							DateTime updatedDate = DateTime.Now;

							PropertyNameValuePair[] properties =
							[
								new PropertyNameValuePair(nameof(FileModel.Contents), contents),
								new PropertyNameValuePair(nameof(FileModel.UpdatedDate), updatedDate)
							];

							if (await _dbAccess.UpdatePropertiesAsync(
								id: parameters.File.Id,
								token: token,
								properties).ConfigureAwait(false))
							{
								_logger.LogDebug(
									"Contents of file is updated in database:" + Environment.NewLine +
									$"File Id = {parameters.File.Id}," + Environment.NewLine +
									$"File path = {parameters.FilePath}," + Environment.NewLine +
									$"Old bytes length = {parameters.Contents.Length}," + Environment.NewLine +
									$"New bytes length = {bytes.Length}.");

								parameters.Contents = [.. bytes];

								parameters
									.File
									.UpdatedDate = updatedDate;
							}
						}
					}
					finally
					{
						if (parameters.SessionEncryptedDek is not null)
						{
							CryptographicOperations.ZeroMemory(bytes);
						}
					}
				}
				finally
				{
					parameters
						.Semaphore
						.Release();
				}

				await Task
					.Delay(800, token)
					.ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			if (parameters.SessionEncryptedDek is not null)
			{
				CryptographicOperations.ZeroMemory(parameters.SessionEncryptedDek);

				CryptographicOperations.ZeroMemory(parameters.Contents);
			}
		}
	}
	#endregion
}
