using DataOrganizer.DTO;
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
public sealed class ExecutionEngine : IExecutionEngine
{
	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

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

	/// <summary>
	/// Returns <c>True</c> if the service was disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public ExecutionEngine(
		IAppEnvironment appEnvironment,
		IFileAssociationService fileAssociation,
		IFileChangeTracker changeTracker,
		IFileSystem fileSystem,
		ILogger logger,
		IProcessUtils processUtils)
	{
		_appEnvironment = appEnvironment;

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

			info
				.Cancellation
				.Cancel();

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

			if (!_isDisposed)
			{
				_semaphore.Release();
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		_semaphore.Dispose();
	}

	/// <inheritdoc />
	public async Task<bool> ExecuteAsync(
		ExecuteFileParameters parameters,
		CancellationToken token = default)
	{
		try
		{
			string directoryPath = Path.Combine(
				_appEnvironment.SandboxDirectoryPath,
				parameters.File.Id.ToString());

			_fileSystem.CreateDirectory(directoryPath);

			// To prevent a directory traversal attack, all directory components must be removed from the file name.
			string fileName = Path.GetFileName(parameters
				.File
				.Name);

			string filePath = Path.Combine(directoryPath, fileName);

			if (AppUtils.IsWindows && _fileAssociation.GetApplicationByExtension(Path.GetExtension(fileName)) is { } appPath)
			{
				_logger.LogDebug($@"Application path to open file ""{fileName}"" is: {appPath}");
			}

			await _fileSystem
				.WriteAllBytesAsync(filePath, parameters.Contents, token)
				.ConfigureAwait(false);

			_fileSystem.SetFileReadOnly(filePath, parameters.IsReadOnly);

			_processUtils.StartProcess(filePath, out int processId);

			CancellationTokenSource cancellation = new();

			_executedFiles.TryAdd(
				parameters.File.Id,
				new(cancellation, filePath, directoryPath, processId));

			if (!parameters.IsReadOnly)
			{
				TrackChangesParameters trackParameters = new()
				{
					Contents = parameters.Contents,
					SessionEncryptedDek = parameters.SessionEncryptedDek,
					File = parameters.File,
					FilePath = filePath,
					Semaphore = _semaphore,
					ViewModel = parameters.ViewModel
				};

				_ = _changeTracker.TrackChangesAsync(trackParameters, cancellation.Token);
			}

			_logger.LogInformation(
				$"The file {filePath} is opened{(parameters.IsReadOnly ? " in read-only mode" : string.Empty)}");

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			_executedFiles.TryRemove(parameters.File.Id, out var _);

			return false;
		}
	}

	/// <inheritdoc />
	public bool IsExecuted(Guid id) => _executedFiles.ContainsKey(id);
	#endregion
}
