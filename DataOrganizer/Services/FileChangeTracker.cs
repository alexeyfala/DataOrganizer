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
			while (!token.IsCancellationRequested && _fileSystem.IsFileExists(parameters.FilePath))
			{
				if (token.IsCancellationRequested || !_fileSystem.IsFileExists(parameters.FilePath))
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

						if (parameters.SessionEncryptedDek is not null)
						{
							if (_entityEcryption.EncryptSessionContents(bytes, parameters.SessionEncryptedDek) is not { } encrypted)
							{
								_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

								return;
							}

							contents = encrypted;
						}

						DateTime updatedDate = DateTime.Now;

						if (await _dbAccess.UpdateFilePropertiesAsync(parameters.File.Id,
							[
								x => x.SetProperty(x => x.Contents, contents),
								x => x.SetProperty(x => x.UpdatedDate, updatedDate)
							], token))
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
						bytes.ZeroMemory();
					}
				}

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
}
