using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Interfaces;
using Entities.Models;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
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

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public FileChangeTracker(
		IDbAccess dbAccess,
		IFileSystem fileSystem,
		ILogger logger)
	{
		_dbAccess = dbAccess;

		_fileSystem = fileSystem;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Tracks changes of the executed file.
	/// </summary>
	public async Task TrackChangesAsync(
		FileModelDto dto,
		string filePath,
		byte[] contents,
		SemaphoreSlim semaphore,
		Predicate<Guid> condition,
		CancellationToken token = default)
	{
		try
		{
			while (_fileSystem.IsFileExists(filePath) && condition(dto.Id))
			{
				try
				{
					await semaphore
						.WaitAsync(token)
						.ConfigureAwait(false);

					if (!_fileSystem.IsFileExists(filePath) || !condition(dto.Id))
					{
						return;
					}

					await using FileStream fileStream = File.Open(
						filePath,
						FileMode.Open,
						FileAccess.Read,
						FileShare.ReadWrite);

					await using MemoryStream memoryStream = new();

					fileStream.CopyTo(memoryStream);

					byte[] bytes = memoryStream.ToArray();

					if (!bytes.SequenceEqual(contents))
					{
						DateTime updatedDate = DateTime.Now;

						PropertyNameValuePair[] properties =
						[
							new PropertyNameValuePair(nameof(FileModel.Contents), bytes),
							new PropertyNameValuePair(nameof(FileModel.UpdatedDate), updatedDate)
						];

						if (await _dbAccess.UpdatePropertiesAsync(
							id: dto.Id,
							token: token,
							properties).ConfigureAwait(false))
						{
							_logger.LogDebug(
								"Contents of file is updated in database:" + Environment.NewLine +
								$"File Id = {dto.Id}," + Environment.NewLine +
								$"File path = {filePath}," + Environment.NewLine +
								$"Old bytes length = {contents.Length}," + Environment.NewLine +
								$"New bytes length = {bytes.Length}.");

							contents = bytes;

							dto.UpdatedDate = updatedDate;
						}
					}
				}
				finally
				{
					semaphore.Release();
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
	}
	#endregion
}
