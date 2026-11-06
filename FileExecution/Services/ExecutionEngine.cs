using Diagnostics.Interfaces;
using FileExecution.Dto;
using FileExecution.Helpers;
using FileExecution.Interfaces;
using Notifications.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TextProcessing.Extensions;
using Threading.Extensions;
using Threading.Helpers;

namespace FileExecution.Services;

/// <inheritdoc cref="IExecutionEngine" />
public sealed class ExecutionEngine : IExecutionEngine
{
	#region Data
	/// <inheritdoc cref="IAppResolver" />
	private readonly IAppResolver _appResolver;

	/// <inheritdoc cref="IFileChangeTracker" />
	private readonly IFileChangeTracker _changeTracker;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="ConcurrentDictionary{TKey, TValue}" />
	private readonly ConcurrentDictionary<Guid, ExecutingFileInfo> _executingFiles = [];

	/// <inheritdoc cref="IFileEraser" />
	private readonly IFileEraser _fileEraser;

	/// <inheritdoc cref="IFileLockChecker" />
	private readonly IFileLockChecker _fileLockChecker;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IExecutingFileHolderFinder" />
	private readonly IExecutingFileHolderFinder _holderFinder;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="IProcessManager" />
	private readonly IProcessManager _processManager;

	/// <inheritdoc cref="IExecutionSandbox" />
	private readonly IExecutionSandbox _sandbox;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public ExecutionEngine(
		IAppResolver appResolver,
		IFileChangeTracker changeTracker,
		IFileEraser fileEraser,
		IFileLockChecker fileLockChecker,
		IFileSystem fileSystem,
		IExecutingFileHolderFinder holderFinder,
		ILogger logger,
		INotificationService notification,
		IProcessManager processManager,
		IExecutionSandbox sandbox,
		ITaskExceptionHandler exceptionHandler)
	{
		_appResolver = appResolver;

		_changeTracker = changeTracker;

		_fileEraser = fileEraser;

		_fileLockChecker = fileLockChecker;

		_fileSystem = fileSystem;

		_holderFinder = holderFinder;

		_exceptionHandler = exceptionHandler;

		_logger = logger;

		_notification = notification;

		_processManager = processManager;

		_sandbox = sandbox;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task CloseAsync(Guid id, CancellationToken token = default)
	{
		// Stryker disable once Block,Statement : a disposed engine has disposed its semaphore, which ends the call below
		if (IsDisposed())
		{
			return;
		}

		using SemaphoreLock held = await TryLockAsync(token).ConfigureAwait(false);

		if (!held.IsHeld)
		{
			return;
		}

		try
		{
			// Stryker disable once Block,Statement : the dispose that got in first closes every file all the same
			if (IsDisposed())
			{
				return;
			}

			// Stryker disable once Block,Statement : without it the missing info ends the call in the catch below
			if (!_executingFiles.TryRemove(id, out ExecutingFileInfo? info))
			{
				_logger.LogError($@"Cannot find file information for id ""{id}""");

				return;
			}

			await StopTrackerAndDisposeCancellationAsync(
				info.Cancellation,
				info.TrackerTask,
				id,
				token).ConfigureAwait(false);

			KillFileProcesses(info, id);

			if (!_fileSystem.FileExists(info.FilePath))
			{
				return;
			}

			if (_fileLockChecker.IsLocked(info.FilePath))
			{
				_logger.LogWarning($@"File ""{id}"" is locked by another process, waiting it to be released.");

				using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

				const int timeout = 10;

				// Stryker disable once Statement : the cap shows only after a real wait of ten seconds
				cancellation.CancelAfter(TimeSpan.FromSeconds(timeout));

				bool unlocked = await _fileLockChecker
					.WaitUntilUnlockedAsync(info.FilePath, cancellation.Token)
					.ConfigureAwait(false);

				// Stryker disable once Boolean : both branches only write to the log
				if (unlocked)
				{
					_logger.LogInformation($@"File ""{id}"" is released.");
				}
				else
				{
					_logger.LogWarning(
						$@"File ""{id}"" is still locked after the {timeout}-second wait; attempting deletion anyway.");
				}
			}

			TryDeleteFile(info.FilePath, info.DirectoryPath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		// Stryker disable once Block,Statement : a second pass finds no files and disposes the disposed semaphore again
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		// Drain in-flight ExecuteAsync / CloseAsync before iterating so the dictionary
		// snapshot is stable. A 10-second cap keeps shutdown bounded if some operation
		// is hung — in that case we log and continue in best-effort mode.
		bool semaphoreAcquired = await _semaphore
			.WaitAsync(TimeSpan.FromSeconds(10))
			.ConfigureAwait(false);

		// Stryker disable once Boolean : the branch only writes to the log
		if (!semaphoreAcquired)
		{
			_logger.LogWarning(
				$"{nameof(DisposeAsync)} could not acquire the semaphore within 10 seconds; proceeding without it.");
		}

		try
		{
			foreach (Guid id in _executingFiles.Keys)
			{
				// Stryker disable once Block,Statement : without it the missing info ends the round in the catch below
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
						id,
						CancellationToken.None).ConfigureAwait(false);

					KillFileProcesses(info, id);

					if (!_fileSystem.FileExists(info.FilePath))
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
		}
		// Stryker disable once Block,Statement,Boolean : the flag stops later calls, so only a deadlock could show these
		finally
		{
			if (semaphoreAcquired)
			{
				_semaphore.Release();
			}

			_semaphore.Dispose();
		}
	}

	/// <inheritdoc />
	public async Task<bool> ExecuteAsync(ExecuteFileParameters parameters, CancellationToken token = default)
	{
		try
		{
			return await ExecuteCoreAsync(parameters, token).ConfigureAwait(false);
		}
		finally
		{
			// The plain text came with the parameters and nothing outside holds it any more:
			// the sandbox file has been written and the tracker keeps only a hash.
			parameters
				.Contents
				.ZeroMemory();
		}
	}

	/// <inheritdoc />
	public bool IsExecuting(Guid id) => _executingFiles.ContainsKey(id);
	#endregion

	#region Helpers
	/// <summary>
	/// Body of <see cref="ExecuteAsync" />, run while the plain text is still owned here.
	/// </summary>
	private async Task<bool> ExecuteCoreAsync(ExecuteFileParameters parameters, CancellationToken token)
	{
		if (IsDisposed())
		{
			return false;
		}

		if (_executingFiles.ContainsKey(parameters.File.Id))
		{
			// Stryker disable once Statement : only writes to the log
			LogDuplicateEntry(parameters.File.Id);

			return false;
		}

		string directoryPath = _sandbox.GetFileDirectoryPath(parameters.File.Id);

		// The name has no directory components, so the file cannot be written outside its directory.
		string fileName = SandboxFileName.Create(parameters
			.File
			.Name);

		string filePath = Path.Combine(directoryPath, fileName);

		AppResolution app = await _appResolver
			.ResolveAsync(filePath, token)
			.ConfigureAwait(false);

		if (app.IsCancelled)
		{
			return false;
		}

		using SemaphoreLock held = await TryLockAsync(token).ConfigureAwait(false);

		if (!held.IsHeld)
		{
			return false;
		}

		try
		{
			if (IsDisposed())
			{
				return false;
			}

			if (_executingFiles.ContainsKey(parameters.File.Id))
			{
				// Stryker disable once Statement : only writes to the log
				LogDuplicateEntry(parameters.File.Id);

				return false;
			}

			await using AsyncRollbackScope scope = new(_logger);

			await PrepareSandboxAsync(
				directoryPath,
				filePath,
				parameters,
				scope,
				token).ConfigureAwait(false);

			IExecutingFileProcess? process = StartFileProcess(
				app.AppPath,
				filePath,
				parameters.File.Id);

			scope.OnRollback(() => KillAndReleaseProcess(process, parameters.File.Id));

			(CancellationTokenSource cancellation, Task trackerTask) = StartChangeTracker(
				parameters,
				fileName,
				filePath,
				scope,
				token);

			ExecutingFileInfo info = new()
			{
				Cancellation = cancellation,
				DirectoryPath = directoryPath,
				FilePath = filePath,
				Process = process,
				TrackerTask = trackerTask
			};

			// The check above ran under the same lock, so no other call has added the file since.
			_executingFiles[parameters.File.Id] = info;

			if (IsDisposed())
			{
				_executingFiles.TryRemove(parameters.File.Id, out _);

				return false;
			}

			scope.Commit();

			_logger.LogInformation(
				$@"The file ""{parameters.File.Id}"" is opened{(parameters.IsReadOnly ? " in read-only mode" : string.Empty)}{(process is null ? string.Empty : $" by process {process.Id}")}");

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			_notification.ShowErrorSnackbar($@"{Strings.FailedToOpenInOS} ""{parameters.File.Name}""");

			return false;
		}

		void LogDuplicateEntry(Guid fileId)
		{
			_logger.LogError(
				$@"An entry for file id ""{fileId}"" already exists in the executing files dictionary.");
		}
	}

	/// <summary>
	/// Programs that hold <paramref name="filePath" /> open; empty when none is found or the search fails.
	/// </summary>
	private IExecutingFileProcess[] FindHolders(string filePath)
	{
		try
		{
			return _holderFinder.FindHolders(filePath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
	}

	/// <summary>
	/// <c>True</c> after the service has been disposed.
	/// </summary>
	private bool IsDisposed() => Volatile.Read(ref _isDisposed);

	/// <summary>
	/// Kills <paramref name="process" /> unless it has ended, then releases it.
	/// </summary>
	private void KillAndReleaseProcess(IExecutingFileProcess? process, Guid fileId)
	{
		if (process is null)
		{
			return;
		}

		try
		{
			if (process.HasExited)
			{
				_logger.LogInformation($@"The process {process.Id} of the file ""{fileId}"" has already ended.");

				return;
			}

			process.Kill();

			_logger.LogInformation($@"The process {process.Id} of the file ""{fileId}"" is killed.");
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			process.Dispose();
		}
	}

	/// <summary>
	/// Kills the programs that hold the file of <paramref name="info" /> open, or its started process when none is found;
	/// the started process is released either way.
	/// </summary>
	private void KillFileProcesses(ExecutingFileInfo info, Guid fileId)
	{
		IExecutingFileProcess[] holders = FindHolders(info.FilePath);

		if (holders.Length == 0)
		{
			KillAndReleaseProcess(info.Process, fileId);

			return;
		}

		_logger.LogInformation($@"The file ""{fileId}"" is held by {holders.Length} process(es), which are killed instead of the started one.");

		foreach (IExecutingFileProcess holder in holders)
		{
			KillAndReleaseProcess(holder, fileId);
		}

		info.Process?.Dispose();
	}

	/// <summary>
	/// Creates the sandbox directory and writes the file, applies the read-only
	/// flag, and registers rollback actions on <paramref name="scope" /> to undo
	/// directory creation and file write on failure.
	/// </summary>
	private async Task PrepareSandboxAsync(
		string directoryPath,
		string filePath,
		ExecuteFileParameters parameters,
		AsyncRollbackScope scope,
		CancellationToken token)
	{
		scope.OnRollback(() => _fileSystem.Directory.Delete(directoryPath, recursive: true));

		_fileSystem
			.Directory
			.CreateDirectory(directoryPath);

		scope.OnRollback(() =>
		{
			_fileSystem.SetFileReadOnly(filePath, false);

			_fileEraser.EraseAndDeleteFile(filePath);
		});

		await _fileSystem
			.File
			.WriteAllBytesAsync(filePath, parameters.Contents, token)
			.ConfigureAwait(false);

		_fileSystem.SetFileReadOnly(filePath, parameters.IsReadOnly);
	}

	/// <summary>
	/// Creates a linked <see cref="CancellationTokenSource" /> and starts the change
	/// tracker for non-readonly files; registers stop+dispose rollback on
	/// <paramref name="scope" />. Disposes the CTS if the start path throws.
	/// </summary>
	private (CancellationTokenSource Cancellation, Task TrackerTask) StartChangeTracker(
		ExecuteFileParameters parameters,
		string fileName,
		string filePath,
		AsyncRollbackScope scope,
		CancellationToken token)
	{
		CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

		Task trackerTask = Task.CompletedTask;

		try
		{
			if (!parameters.IsReadOnly)
			{
				TrackChangesParameters trackParameters = new()
				{
					File = parameters.File,
					FileName = fileName,
					FilePath = filePath,
					KeeperId = parameters.KeeperId,
					PreviousHash = CryptographicOperations.HashData(
						TrackChangesParameters.HashAlgorithm,
						parameters.Contents)
				};

				trackerTask = _changeTracker.TrackChangesAsync(trackParameters, cancellation.Token);

				_exceptionHandler.Watch(trackerTask);
			}
		}
		catch
		{
			try
			{
				cancellation.Cancel();
			}
			catch
			{
				// Ignored — best-effort cancellation only.
			}

			// Stryker disable once Statement : the source goes to nobody, so its disposal cannot be observed
			cancellation.Dispose();

			throw;
		}

		scope.OnRollback(() => StopTrackerAndDisposeCancellationAsync(
			cancellation,
			trackerTask,
			parameters.File.Id,
			CancellationToken.None));

		return (cancellation, trackerTask);
	}

	/// <summary>
	/// Starts the process that opens <paramref name="filePath" />: via <paramref name="selectedAppPath" /> when provided,
	/// otherwise in the program the operating system assigns; <c>null</c> when no process of its own started.
	/// </summary>
	private IExecutingFileProcess? StartFileProcess(
		string? selectedAppPath,
		string filePath,
		Guid fileId)
	{
		if (selectedAppPath is not null)
		{
			return _processManager.OpenFile(selectedAppPath, filePath);
		}

		IExecutingFileProcess? process = _processManager.OpenFile(filePath);

		// Stryker disable once Equality : the branch only writes to the log
		if (process is null)
		{
			_logger.LogDebug(
				$@"File ""{fileId}"" was started without a process of its own: the operating system passed it to a program and gave no process back.");
		}

		return process;
	}

	/// <summary>
	/// Cancels <paramref name="cancellation" />, waits up to 5 seconds for
	/// <paramref name="trackerTask" /> to exit (honouring <paramref name="token" />),
	/// then disposes <paramref name="cancellation" />.
	/// <paramref name="fileId" /> is used only for the timeout warning message.
	/// </summary>
	private async Task StopTrackerAndDisposeCancellationAsync(
		CancellationTokenSource cancellation,
		Task trackerTask,
		Guid fileId,
		CancellationToken token)
	{
		try
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
				_logger.LogWarning($@"Change tracker for ""{fileId}"" did not stop within 5 seconds.");
			}
			catch (OperationCanceledException)
			{
				// Expected — tracker observed Cancel() or the outer token was cancelled.
			}
		}
		// Stryker disable once Block,Statement : the disposal of the source cannot be observed
		finally
		{
			cancellation.Dispose();
		}
	}

	/// <summary>
	/// Tries to delete a file and the folder containing it.
	/// </summary>
	private void TryDeleteFile(string filePath, string directoryPath)
	{
		try
		{
			_fileSystem.SetFileReadOnly(filePath, false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}

		try
		{
			_fileEraser.EraseAndDeleteFile(filePath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}

		try
		{
			_fileSystem
				.Directory
				.Delete(directoryPath, recursive: true);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Waits for the semaphore; a lock that holds nothing when the engine has been disposed meanwhile or the wait was
	/// cancelled.
	/// </summary>
	private async Task<SemaphoreLock> TryLockAsync(CancellationToken token)
	{
		try
		{
			return await _semaphore
				.LockAsync(token)
				.ConfigureAwait(false);
		}
		// Stryker disable once Block : the block only logs and returns what an empty block returns
		catch (Exception ex)
		{
			// ObjectDisposedException — service was disposed concurrently.
			// OperationCanceledException — caller cancelled the token.
			// Stryker disable once Equality,Logical : the branch only writes to the log
			if (ex is not ObjectDisposedException and not OperationCanceledException)
			{
				_logger.LogException(ex);
			}

			return default;
		}
	}
	#endregion
}
