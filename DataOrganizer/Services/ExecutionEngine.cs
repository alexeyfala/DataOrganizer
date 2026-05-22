using DataOrganizer.DTO;
using DataOrganizer.Helpers;
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
	private readonly ConcurrentDictionary<Guid, ExecutingFileInfo> _executingFiles = [];

	/// <inheritdoc cref="IFileAssociationService" />
	private readonly IFileAssociationService _fileAssociation;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _handler;

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
		ITaskExceptionHandler handler,
		IProcessUtils processUtils,
		ILogger logger,
		IFileSystem fileSystem,
		IFileChangeTracker changeTracker,
		IFileAssociationService fileAssociation,
		IAppEnvironment appEnvironment)
	{
		_appEnvironment = appEnvironment;

		_changeTracker = changeTracker;

		_fileAssociation = fileAssociation;

		_fileSystem = fileSystem;

		_handler = handler;

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

			if (!_executingFiles.TryRemove(id, out ExecutingFileInfo? info))
			{
				_logger.LogError($@"Cannot find file information for id ""{id}""");

				return;
			}

			await StopTrackerAndDisposeCancellationAsync(
				info.Cancellation,
				info.TrackerTask,
				info.FilePath,
				token).ConfigureAwait(false);

			if (!TryKillProcess(
				id,
				info.ProcessId,
				info.FilePath))
			{
				return;
			}

			if (!_fileSystem.IsFileExists(info.FilePath))
			{
				return;
			}

			if (_fileSystem.IsFileLocked(info.FilePath))
			{
				_logger.LogWarning($@"File ""{info.FilePath}"" is locked by another process, waiting it to be released.");

				using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

				cancellation.CancelAfter(TimeSpan.FromSeconds(30));

				await _fileSystem
					.WaitFileLockedAsync(info.FilePath, token: cancellation.Token)
					.ConfigureAwait(false);

				_logger.LogInformation($@"File ""{info.FilePath}"" is released.");
			}

			TryDeleteFile(info.FilePath, info.DirectoryPath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		foreach (Guid id in _executingFiles.Keys)
		{
			if (!_executingFiles.TryRemove(id, out ExecutingFileInfo? info))
			{
				// A concurrent CloseAsync got there first and owns this entry now.
				continue;
			}

			try
			{
				await StopTrackerAndDisposeCancellationAsync(
					info.Cancellation,
					info.TrackerTask,
					info.FilePath,
					CancellationToken.None).ConfigureAwait(false);

				if (!TryKillProcess(id, info.ProcessId, info.FilePath))
				{
					continue;
				}

				TryDeleteFile(info.FilePath, info.DirectoryPath);
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);
			}
		}

		_semaphore.Dispose();
	}

	/// <inheritdoc />
	public async Task<bool> ExecuteAsync(ExecuteFileParameters parameters, CancellationToken token = default)
	{
		await using AsyncRollbackScope scope = new(_logger);

		try
		{
			string directoryPath = Path.Combine(
				_appEnvironment.SandboxDirectoryPath,
				parameters.File.Id.ToString());

			_fileSystem.CreateDirectory(directoryPath);

			scope.OnRollback(() => _fileSystem.DeleteDirectory(directoryPath));

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

			scope.OnRollback(() =>
			{
				_fileSystem.SetFileReadOnly(filePath, false);

				_fileSystem.EraseAndDeleteFile(filePath);
			});

			_fileSystem.SetFileReadOnly(filePath, parameters.IsReadOnly);

			// The method StartProcess may return false and 0 in the process ID
			// if the file has no extension or the extension does not have an application
			// associated with it in the operating system.
			_ = _processUtils.StartProcess(filePath, out int processId);

			scope.OnRollback(() =>
			{
				if (processId.IsNotDefault() && _processUtils.IsProcessExists(processId))
				{
					_processUtils.KillProcess(processId);
				}
			});

			CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

			Task trackerTask = Task.CompletedTask;

			scope.OnRollback(() => StopTrackerAndDisposeCancellationAsync(
				cancellation,
				trackerTask,
				filePath,
				CancellationToken.None));

			if (!parameters.IsReadOnly)
			{
				TrackChangesParameters trackParameters = new()
				{
					Contents = parameters.Contents,
					File = parameters.File,
					FileName = fileName,
					FilePath = filePath,
					SessionEncryptedDek = parameters.SessionEncryptedDek
				};

				trackerTask = _changeTracker.TrackChangesAsync(trackParameters, cancellation.Token);

				_handler.Watch(trackerTask);
			}

			ExecutingFileInfo info = new()
			{
				Cancellation = cancellation,
				DirectoryPath = directoryPath,
				FilePath = filePath,
				ProcessId = processId,
				TrackerTask = trackerTask
			};

			_logger.LogInformation(
				$"The file {filePath} is opened{(parameters.IsReadOnly ? " in read-only mode" : string.Empty)}");

			if (!_executingFiles.TryAdd(parameters.File.Id, info))
			{
				_logger.LogError(
					$@"An entry for file id ""{parameters.File.Id}"" already exists in the executing files dictionary.");

				return false;
			}

			scope.Commit();

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
	}

	/// <inheritdoc />
	public bool IsExecuting(Guid id) => _executingFiles.ContainsKey(id);
	#endregion

	#region Service
	/// <summary>
	/// Cancels <paramref name="cancellation" />, waits up to 5 seconds for
	/// <paramref name="trackerTask" /> to exit (honouring <paramref name="token" />),
	/// then disposes <paramref name="cancellation" />.
	/// <paramref name="filePath" /> is used only for the timeout warning message.
	/// </summary>
	private async Task StopTrackerAndDisposeCancellationAsync(
		CancellationTokenSource cancellation,
		Task trackerTask,
		string filePath,
		CancellationToken token)
	{
		await cancellation
			.CancelAsync()
			.ConfigureAwait(false);

		try
		{
			await trackerTask
				.WaitAsync(TimeSpan.FromSeconds(5), token)
				.ConfigureAwait(false);
		}
		catch (TimeoutException)
		{
			_logger.LogWarning($@"Change tracker for ""{filePath}"" did not stop within 5 seconds.");
		}
		catch (OperationCanceledException)
		{
			// Expected — tracker observed Cancel() or the outer token was cancelled.
		}

		cancellation.Dispose();
	}

	/// <summary>
	/// Tries to delete a file and the folder containing it.
	/// </summary>
	private void TryDeleteFile(string filePath, string directoryPath)
	{
		try
		{
			_fileSystem.SetFileReadOnly(filePath, false);

			_fileSystem.EraseAndDeleteFile(filePath);

			_fileSystem.DeleteDirectory(directoryPath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Returns <c>True</c> if the file is exists and tries to kill the process of it.
	/// </summary>
	private bool TryKillProcess(
		Guid fileId,
		int processId,
		string filePath)
	{
		if (!_fileSystem.IsFileExists(filePath))
		{
			_logger.LogError($@"The file with id ""{fileId}"" does not exist ""{filePath}""", false);

			return false;
		}

		if (processId.IsNotDefault() && _processUtils.IsProcessExists(processId))
		{
			try
			{
				_processUtils.KillProcess(processId);
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);
			}
		}

		return true;
	}
	#endregion
}
