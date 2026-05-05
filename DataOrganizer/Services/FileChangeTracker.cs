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

	/// <inheritdoc cref="IViewModelExecutionService" />
	private readonly IViewModelExecutionService _viewModel;
	#endregion

	#region Constructors
	public FileChangeTracker(
		IDbAccess dbAccess,
		IEntityEcryption entityEcryption,
		IFileSystem fileSystem,
		ILogger logger,
		IViewModelExecutionService viewModel)
	{
		_dbAccess = dbAccess;

		_entityEcryption = entityEcryption;

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
			await using FileStream initial = CreateFileStream(parameters.FilePath);

			byte[] previousHash = await ComputeSha256HashAsync(initial, token).ConfigureAwait(false);

			while (!token.IsCancellationRequested && _fileSystem.IsFileExists(parameters.FilePath))
			{
				if (token.IsCancellationRequested || !_fileSystem.IsFileExists(parameters.FilePath))
				{
					return;
				}

				await using FileStream currentStream = CreateFileStream(parameters.FilePath);

				byte[] currentHash = await ComputeSha256HashAsync(currentStream, token).ConfigureAwait(false);

				if (!currentHash.SequenceEqual(previousHash))
				{
					currentStream.Position = 0;

					await using MemoryStream memoryStream = new();

					await currentStream
						.CopyToAsync(memoryStream, token)
						.ConfigureAwait(false);

					byte[] bytes = memoryStream.ToArray();

					try
					{
						if (parameters.SessionEncryptedDek is not null)
						{
							if (_entityEcryption.EncryptSessionContents(bytes, parameters.SessionEncryptedDek) is not { } encrypted)
							{
								_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

								return;
							}

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
						if (parameters.SessionEncryptedDek is not null)
						{
							bytes.ZeroMemory();
						}
					}
				}

				previousHash = currentHash;

				await Task
					.Delay(800, token)
					.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
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
				parameters
					.SessionEncryptedDek
					.ZeroMemory();

				parameters
					.Contents
					.ZeroMemory();
			}
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Computes the <see cref="HashAlgorithmName.SHA256" /> hash of <see cref="Stream" /> content.
	/// </summary>
	private static ValueTask<byte[]> ComputeSha256HashAsync(Stream stream, CancellationToken token = default)
	{
		return CryptographicOperations.HashDataAsync(
			HashAlgorithmName.SHA256,
			stream,
			token);
	}

	/// <summary>
	/// Creates a <see cref="FileStream" /> with certain settings.
	/// </summary>
	private static FileStream CreateFileStream(string filePath)
	{
		return File.Open(
			filePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite);
	}
	#endregion
}
