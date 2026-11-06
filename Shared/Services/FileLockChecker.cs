using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Shared.Services;

/// <inheritdoc cref="IFileLockChecker" />
public sealed class FileLockChecker : IFileLockChecker
{
	#region Data
	/// <summary>
	/// Pause between two checks of a locked file.
	/// </summary>
	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IPlatform" />
	private readonly IPlatform _platform;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public FileLockChecker(
		IFileSystem fileSystem,
		IPlatform platform,
		TimeProvider timeProvider)
	{
		_fileSystem = fileSystem;

		_platform = platform;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public bool IsLocked(string filePath)
	{
		try
		{
			using Stream stream = _fileSystem
				.File
				.Open(
					filePath,
					FileMode.Open,
					FileAccess.Read,
					FileShare.None);
		}
		catch (IOException ex) when (_platform.IsWindows && (ex.HResult & 0x0000FFFF) == 32)
		{
			return true;
		}
		catch (IOException) when (!_platform.IsWindows)
		{
			// On Unix .NET maps FileShare.None to an advisory fcntl lock, so an IOException here means another process
			// holds the lock. Most Unix apps set no such lock, so a holder may go unnoticed.
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public async ValueTask<bool> WaitUntilUnlockedAsync(string filePath, CancellationToken token = default)
	{
		while (_fileSystem.FileExists(filePath) && IsLocked(filePath))
		{
			if (token.IsCancellationRequested)
			{
				return false;
			}

			await Task
				.Delay(PollInterval, _timeProvider, token)
				.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		}

		return true;
	}
	#endregion
}
