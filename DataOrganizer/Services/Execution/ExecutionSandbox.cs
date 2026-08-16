using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Execution;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Execution;

/// <inheritdoc cref="IExecutionSandbox" />
public sealed class ExecutionSandbox : IExecutionSandbox
{
	#region Data
	/// <summary>
	/// Number of attempts to remove the folder.
	/// </summary>
	private const int MaxAttemptCount = 10;

	/// <summary>
	/// Pause between the attempts to remove the folder.
	/// </summary>
	private static readonly TimeSpan AttemptDelay = TimeSpan.FromMilliseconds(300);

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public ExecutionSandbox(
		IAppEnvironment appEnvironment,
		IFileSystem fileSystem,
		ILogger logger,
		TimeProvider timeProvider)
	{
		_appEnvironment = appEnvironment;

		_fileSystem = fileSystem;

		_logger = logger;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Properties
	/// <summary>
	/// Path of the folder.
	/// </summary>
	private string DirectoryPath => _appEnvironment.SandboxDirectoryPath;
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task EraseAsync(CancellationToken token = default)
	{
		for (int attempt = 1; attempt <= MaxAttemptCount; attempt++)
		{
			if (!_fileSystem.IsDirectoryExists(DirectoryPath))
			{
				return;
			}

			if (attempt > 1)
			{
				await Task
					.Delay(AttemptDelay, _timeProvider, token)
					.ConfigureAwait(false);
			}

			try
			{
				_fileSystem.EraseAndDeleteDirectory(DirectoryPath);

				_logger.LogInformation($@"Folder ""{DirectoryPath}"" is erased");

				return;
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);
			}
		}

		_logger.LogWarning($@"Can't erase folder ""{DirectoryPath}"" with {MaxAttemptCount} attempts");
	}

	/// <inheritdoc />
	public string GetFileDirectoryPath(Guid fileId) => Path.Combine(DirectoryPath, fileId.ToString());
	#endregion
}
