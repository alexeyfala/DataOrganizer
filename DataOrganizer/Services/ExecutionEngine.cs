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

	/// <inheritdoc cref="IAppPickerService" />
	private readonly IAppPickerService _appPicker;

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
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public ExecutionEngine(
		IAppEnvironment appEnvironment,
		IAppPickerService appPicker,
		IFileAssociationService fileAssociation,
		IFileChangeTracker changeTracker,
		IFileSystem fileSystem,
		ILogger logger,
		IProcessUtils processUtils,
		ITaskExceptionHandler handler)
	{
		_appEnvironment = appEnvironment;

		_appPicker = appPicker;

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
		if (Volatile.Read(ref _isDisposed))
		{
			return;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// ObjectDisposedException — service was disposed concurrently.
			// OperationCanceledException — caller cancelled the token.
			if (ex is not ObjectDisposedException and not OperationCanceledException)
			{
				_logger.LogException(ex);
			}

			return;
		}

		try
		{
			if (Volatile.Read(ref _isDisposed))
			{
				return;
			}

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

			TryKillProcess(info.ProcessId);

			if (!_fileSystem.IsFileExists(info.FilePath))
			{
				return;
			}

			if (_fileSystem.IsFileLocked(info.FilePath))
			{
				_logger.LogWarning($@"File ""{info.FilePath}"" is locked by another process, waiting it to be released.");

				using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

				const int timeout = 10;

				cancellation.CancelAfter(TimeSpan.FromSeconds(timeout));

				bool unlocked = await _fileSystem
					.WaitFileUnlockedAsync(info.FilePath, token: cancellation.Token)
					.ConfigureAwait(false);

				if (unlocked)
				{
					_logger.LogInformation($@"File ""{info.FilePath}"" is released.");
				}
				else
				{
					_logger.LogWarning(
						$@"File ""{info.FilePath}"" is still locked after the {timeout}-second wait; attempting deletion anyway.");
				}
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
				// Service was disposed concurrently.
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

		// Drain in-flight ExecuteAsync / CloseAsync before iterating so the dictionary
		// snapshot is stable. A 10-second cap keeps shutdown bounded if some operation
		// is hung — in that case we log and continue in best-effort mode.
		bool semaphoreAcquired;

		try
		{
			semaphoreAcquired = await _semaphore
				.WaitAsync(TimeSpan.FromSeconds(10))
				.ConfigureAwait(false);
		}
		catch (ObjectDisposedException)
		{
			semaphoreAcquired = false;
		}

		if (!semaphoreAcquired)
		{
			_logger.LogWarning(
				$"{nameof(DisposeAsync)} could not acquire the semaphore within 10 seconds; proceeding without it.");
		}

		try
		{
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

					TryKillProcess(info.ProcessId);

					if (!_fileSystem.IsFileExists(info.FilePath))
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
		finally
		{
			if (semaphoreAcquired)
			{
				try
				{
					_semaphore.Release();
				}
				catch (Exception ex) when (ex is ObjectDisposedException or SemaphoreFullException)
				{
				}
			}

			_semaphore.Dispose();
		}
	}

	/// <inheritdoc />
	public async Task<bool> ExecuteAsync(ExecuteFileParameters parameters, CancellationToken token = default)
	{
		if (Volatile.Read(ref _isDisposed))
		{
			return false;
		}

		if (_executingFiles.ContainsKey(parameters.File.Id))
		{
			LogDuplicateEntry(parameters.File.Id);

			return false;
		}

		string directoryPath = Path.Combine(
			_appEnvironment.SandboxDirectoryPath,
			parameters.File.Id.ToString());

		// To prevent a directory traversal attack, all directory components must be removed from the file name.
		string fileName = Path.GetFileName(parameters
			.File
			.Name);

		string filePath = Path.Combine(directoryPath, fileName);

		(bool shouldContinue, string? selectedAppPath) = await TryResolveAppPathAsync(
			fileName,
			filePath,
			token).ConfigureAwait(false);

		if (!shouldContinue)
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// ObjectDisposedException — service was disposed concurrently.
			// OperationCanceledException — caller cancelled the token.
			if (ex is not ObjectDisposedException and not OperationCanceledException)
			{
				_logger.LogException(ex);
			}

			return false;
		}

		try
		{
			if (Volatile.Read(ref _isDisposed))
			{
				return false;
			}

			if (_executingFiles.ContainsKey(parameters.File.Id))
			{
				LogDuplicateEntry(parameters.File.Id);

				return false;
			}

			await using AsyncRollbackScope scope = new(_logger);

			scope.OnRollback(() => _fileSystem.DeleteDirectory(directoryPath));

			_fileSystem.CreateDirectory(directoryPath);

			scope.OnRollback(() =>
			{
				_fileSystem.SetFileReadOnly(filePath, false);

				_fileSystem.EraseAndDeleteFile(filePath);
			});

			await _fileSystem
				.WriteAllBytesAsync(filePath, parameters.Contents, token)
				.ConfigureAwait(false);

			_fileSystem.SetFileReadOnly(filePath, parameters.IsReadOnly);

			int processId;

			if (selectedAppPath is not null)
			{
				_ = _processUtils.StartProcess(selectedAppPath, filePath, out processId);
			}
			else if (!_processUtils.StartProcess(filePath, out processId))
			{
				_logger.LogDebug(
					$@"File ""{filePath}"" was opened without an associated process — no extension or no system association.");
			}

			scope.OnRollback(() => TryKillProcess(processId));

			CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

			Task trackerTask = Task.CompletedTask;

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

			scope.OnRollback(() => StopTrackerAndDisposeCancellationAsync(
				cancellation,
				trackerTask,
				filePath,
				CancellationToken.None));

			ExecutingFileInfo info = new()
			{
				Cancellation = cancellation,
				DirectoryPath = directoryPath,
				FilePath = filePath,
				ProcessId = processId,
				TrackerTask = trackerTask
			};

			if (!_executingFiles.TryAdd(parameters.File.Id, info))
			{
				LogDuplicateEntry(parameters.File.Id);

				return false;
			}

			if (Volatile.Read(ref _isDisposed))
			{
				_executingFiles.TryRemove(parameters.File.Id, out _);

				return false;
			}

			scope.Commit();

			_logger.LogInformation(
				$@"The file ""{filePath}"" is opened{(parameters.IsReadOnly ? " in read-only mode" : string.Empty)}");

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently.
			}
		}

		void LogDuplicateEntry(Guid fileId)
		{
			_logger.LogError(
				$@"An entry for file id ""{fileId}"" already exists in the executing files dictionary.");
		}
	}

	/// <inheritdoc />
	public bool IsExecuting(Guid id) => _executingFiles.ContainsKey(id);
	#endregion

	#region Helpers
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
				_logger.LogWarning($@"Change tracker for ""{filePath}"" did not stop within 5 seconds.");
			}
			catch (OperationCanceledException)
			{
				// Expected — tracker observed Cancel() or the outer token was cancelled.
			}
		}
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

			_fileSystem.EraseAndDeleteFile(filePath);

			_fileSystem.DeleteDirectory(directoryPath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Tries to kill the process by ID if it is not default.
	/// </summary>
	private void TryKillProcess(int processId)
	{
		if (processId.IsDefault() || !_processUtils.IsProcessExists(processId))
		{
			return;
		}

		try
		{
			_processUtils.KillProcess(processId);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// On Windows: resolves which application should open the file. Returns the user's
	/// pick when the system association is missing or points at "OpenWith.exe", or
	/// <c>null</c> selectedAppPath when the default shell-execute path is fine. The
	/// bool flag is <c>false</c> only when the user cancelled the picker.
	/// </summary>
	private async Task<(bool ShouldContinue, string? SelectedAppPath)> TryResolveAppPathAsync(
		string fileName,
		string filePath,
		CancellationToken token)
	{
		if (!AppUtils.IsWindows)
		{
			return (true, null);
		}

		string? appPath = _fileAssociation.GetApplicationByExtension(Path.GetExtension(fileName));

		if (appPath?.EndsWith("OpenWith.exe", StringComparison.OrdinalIgnoreCase) == false)
		{
			_logger.LogDebug($@"Application path to open file ""{fileName}"" is: {appPath}");

			return (true, null);
		}

		AssociatedAppInfo? selected = await _appPicker
			.PickAppAsync(filePath, token)
			.ConfigureAwait(false);

		if (selected is null)
		{
			_logger.LogInformation($@"User cancelled the application picker for ""{filePath}"".");

			return (false, null);
		}

		return (true, selected.AppPath);
	}
	#endregion
}
