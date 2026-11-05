using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using LibraryFileSystem = System.IO.Abstractions.IFileSystem;

namespace Shared.Services;

public sealed class FileSystem : IFileSystem
{
	#region Data
	/// <summary>
	/// Size of the buffer of a sequential stream.
	/// </summary>
	private const int SequentialBufferSize = 81920;

	/// <summary>
	/// File system the calls are passed to.
	/// </summary>
	private readonly LibraryFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;

	/// <inheritdoc cref="IPlatform" />
	private readonly IPlatform _platform;
	#endregion

	#region Constructors
	public FileSystem(
		LibraryFileSystem fileSystem,
		IJsonSerializer jsonSerializer,
		IPlatform platform)
	{
		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_platform = platform;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public ValueTask<byte[]> ComputeStreamHashAsync(
		HashAlgorithmName algorithm,
		Stream stream,
		CancellationToken token = default)
	{
		return CryptographicOperations.HashDataAsync(
			algorithm,
			stream,
			token);
	}

	/// <inheritdoc />
	public void CreateDirectory(string directoryPath)
	{
		_fileSystem
			.Directory
			.CreateDirectory(directoryPath);
	}

	/// <inheritdoc />
	public Stream CreateSequentialWrite(string filePath)
	{
		return _fileSystem
			.FileStream
			.New(
				filePath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				SequentialBufferSize,
				FileOptions.Asynchronous);
	}

	/// <inheritdoc />
	public void DeleteDirectory(string directoryPath, bool recursive = true)
	{
		_fileSystem
			.Directory
			.Delete(directoryPath, recursive);
	}

	/// <inheritdoc />
	public bool DirectoryExists([NotNullWhen(true)] string? directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath) || !_fileSystem.Directory.Exists(directoryPath))
		{
			return false;
		}

		if (Path.GetDirectoryName(directoryPath) is not { } parentPath)
		{
			return true;
		}

		try
		{
			string directoryName = Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

			if (string.IsNullOrEmpty(directoryName))
			{
				return true;
			}

			return _fileSystem
				.Directory
				.EnumerateDirectories(parentPath)
				.Any(x => string.Equals(Path.GetFileName(x), directoryName, StringComparison.Ordinal));
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);

			return false;
		}
	}

	/// <inheritdoc />
	public IEnumerable<string> EnumerateFiles(string directoryPath)
	{
		return _fileSystem
			.Directory
			.EnumerateFiles(directoryPath);
	}

	/// <inheritdoc />
	public bool FileExists([NotNullWhen(true)] string? filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !_fileSystem.File.Exists(filePath))
		{
			return false;
		}

		if (Path.GetDirectoryName(filePath) is not { } parentPath)
		{
			return true;
		}

		try
		{
			string fileName = Path.GetFileName(filePath);

			if (string.IsNullOrEmpty(fileName))
			{
				return false;
			}

			return _fileSystem
				.Directory
				.EnumerateFiles(parentPath)
				.Any(x => string.Equals(Path.GetFileName(x), fileName, StringComparison.Ordinal));
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);

			return false;
		}
	}

	/// <inheritdoc />
	public bool IsFileLocked(string filePath)
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
			// On Unix .NET maps FileShare.None to an advisory fcntl lock,
			// so an IOException here means another process holds the lock.
			// Note: most Unix apps do not set advisory locks, so a false
			// negative is expected — caller should additionally rely on
			// size/LastWriteTime stabilization where it matters.
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public Stream OpenRead(string filePath)
	{
		return _fileSystem
			.File
			.Open(
				filePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete);
	}

	/// <inheritdoc />
	public Stream OpenSequentialRead(string filePath)
	{
		return _fileSystem
			.FileStream
			.New(
				filePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				SequentialBufferSize,
				FileOptions.Asynchronous | FileOptions.SequentialScan);
	}

	/// <inheritdoc />
	public byte[] ReadAllBytes(string filePath)
	{
		return _fileSystem
			.File
			.ReadAllBytes(filePath);
	}

	/// <inheritdoc />
	public Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken token = default)
	{
		return _fileSystem
			.File
			.ReadAllBytesAsync(filePath, token);
	}

	/// <inheritdoc />
	public string ReadAllText(string filePath)
	{
		return _fileSystem
			.File
			.ReadAllText(filePath);
	}

	/// <inheritdoc />
	public void SerializeToJsonFile<T>(
		T value,
		string filePath,
		bool isHidden)
	{
		if (FileExists(filePath))
		{
			SetFileHidden(filePath, false);
		}

		if (Path.GetDirectoryName(filePath) is { } parentDirectory)
		{
			CreateDirectory(parentDirectory);
		}

		WriteAllText(filePath, _jsonSerializer.Serialize(value, JsonDefaults.Options));

		if (!isHidden)
		{
			return;
		}

		SetFileHidden(filePath, true);
	}

	/// <inheritdoc />
	public void SetFileHidden(string filePath, bool isHidden) => _fileSystem.SetFileHidden(filePath, isHidden);

	/// <inheritdoc />
	public void SetFileReadOnly(string filePath, bool isReadOnly) => _fileSystem.SetFileReadOnly(filePath, isReadOnly);

	/// <inheritdoc />
	[UnsupportedOSPlatform("windows")]
	public void SetUnixFileMode(string path, UnixFileMode mode)
	{
		_fileSystem
			.File
			.SetUnixFileMode(path, mode);
	}

	/// <inheritdoc />
	public async ValueTask<bool> WaitUntilFileUnlockedAsync(
		string filePath,
		ILogger? logger = null,
		CancellationToken token = default)
	{
		while (FileExists(filePath) && IsFileLocked(filePath))
		{
			if (token.IsCancellationRequested)
			{
				return false;
			}

			logger?.LogWarning($@"File ""{filePath}"" is locked by another process, waiting it to be released.");

			await Task
				.Delay(500, token)
				.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		}

		return true;
	}

	/// <inheritdoc />
	public Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default)
	{
		return _fileSystem
			.File
			.WriteAllBytesAsync(
				filePath,
				bytes,
				token);
	}

	/// <inheritdoc />
	public void WriteAllText(string filePath, string? contents)
	{
		_fileSystem
			.File
			.WriteAllText(filePath, contents);
	}
	#endregion
}
