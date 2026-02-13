using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IExecutionEngine" />
public class ExecutionEngine : IExecutionEngine
{
	#region Data
	/// <inheritdoc cref="IFileChangeTracker" />
	private readonly IFileChangeTracker _changeTracker;

	/// <inheritdoc cref="ConcurrentDictionary{TKey, TValue}" />
	private readonly ConcurrentDictionary<Guid, ExecutedFileInfo> _executedFiles = [];

	/// <inheritdoc cref="IFileAssociationService" />
	private readonly IFileAssociationService _fileAssociation;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IProcessUtils" />
	private readonly IProcessUtils _processUtils;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	#endregion

	#region Constructors
	public ExecutionEngine(
		IFileChangeTracker changeTracker,
		IFileAssociationService fileAssociation,
		IFileSystem fileSystem,
		ILogger logger,
		IProcessUtils processUtils)
	{
		_changeTracker = changeTracker;

		_fileAssociation = fileAssociation;

		_fileSystem = fileSystem;

		_logger = logger;

		_processUtils = processUtils;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task CloseAsync(Guid id, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			if (!_executedFiles.TryGetValue(id, out ExecutedFileInfo? info))
			{
				_logger.LogError($@"Cannot find file information for id ""{id}""");

				return;
			}

			if (!_fileSystem.IsFileExists(info.FilePath))
			{
				_logger.LogError($@"The file with id ""{id}"" does not exist ""{info.FilePath}""");

				return;
			}

			if (info.ProcessId.IsNotDefault() && _processUtils.IsProcessExists(info.ProcessId))
			{
				try
				{
					_processUtils.KillProcess(info.ProcessId);
				}
				catch (Exception ex)
				{
					_logger.LogException(ex);
				}
			}

			if (_fileSystem.IsFileLocked(info.FilePath))
			{
				_logger.LogWarning($@"File ""{info.FilePath}"" is locked by another process, waiting it to be released.");

				await _fileSystem
					.WaitWhileFileIsLockedAsync(info.FilePath, token: token)
					.ConfigureAwait(false);

				_logger.LogInformation($@"File ""{info.FilePath}"" is released.");
			}

			_fileSystem.SetFileReadOnly(info.FilePath, false);

			_fileSystem.EraseAndDeleteFile(info.FilePath);

			_fileSystem.DeleteDirectory(info.DirectoryPath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			_executedFiles.TryRemove(id, out var _);

			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> ExecuteAsync(
		FileModelDto dto,
		byte[] contents,
		bool isReadOnly,
		CancellationToken token = default)
	{
		try
		{
			string directoryPath = Path.Combine(
				AppUtils.SandboxDirectoryPath,
				dto.Id.ToString());

			_fileSystem.CreateDirectory(directoryPath);

			string filePath = Path.Combine(directoryPath, dto.Name);

			if (_fileAssociation.GetApplicationByExtension(Path.GetExtension(dto.Name)) is { } appPath)
			{
				_logger.LogDebug($@"Application path to open file ""{dto.Name}"" is: {appPath}");
			}

			if (dto.EncryptionStatus == EncryptionStatus.Decrypted
				&& dto.FindParent(x => x.EncryptedPassword is not null)?.EncryptedPassword is { } password)
			{

			}

			await _fileSystem
				.WriteAllBytesAsync(filePath, contents, token)
				.ConfigureAwait(false);

			_fileSystem.SetFileReadOnly(filePath, isReadOnly);

			_processUtils.StartProcess(filePath, out int processId);

			_executedFiles.TryAdd(dto.Id, new(filePath, directoryPath, processId));

			if (!isReadOnly)
			{
				_ = _changeTracker.TrackChangesAsync(
					dto: dto,
					filePath: filePath,
					contents: contents,
					semaphore: _semaphore,
					condition: _executedFiles.ContainsKey,
					token: token);
			}

			_logger.LogInformation(
				$@"The file {filePath} is opened{(isReadOnly ? " in read-only mode" : string.Empty)}");

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			_executedFiles.TryRemove(dto.Id, out var _);

			return false;
		}
	}

	/// <inheritdoc />
	public bool IsExecuted(Guid id) => _executedFiles.ContainsKey(id);
	#endregion
}
