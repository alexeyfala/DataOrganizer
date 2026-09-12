using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Services;

public sealed class FileSystem : IFileSystem
{
	#region Data
	/// <summary>
	/// Extension of the file an atomic write is prepared in.
	/// </summary>
	private const string TemporaryFileExtension = ".tmp";

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;
	#endregion

	#region Constructors
	public FileSystem(IJsonSerializer jsonSerializer) => _jsonSerializer = jsonSerializer;
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
	public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);

	/// <inheritdoc />
	public Stream CreateSequentialWrite(string filePath) => new FileStream(
		filePath,
		FileMode.Create,
		FileAccess.Write,
		FileShare.None,
		bufferSize: 81920,
		options: FileOptions.Asynchronous);

	/// <inheritdoc />
	public void DeleteDirectory(string directoryPath, bool recursive = true)
	{
		Directory.Delete(directoryPath, recursive);
	}

	/// <inheritdoc />
	public bool DirectoryExists([NotNullWhen(true)] string? directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
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

			return Directory
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
		return Directory.EnumerateFiles(directoryPath);
	}

	/// <inheritdoc />
	public void EraseAndDeleteDirectory(string directoryPath)
	{
		List<Exception> failures = [];

		foreach (string filePath in Directory.EnumerateFiles(
			directoryPath,
			"*",
			SearchOption.AllDirectories))
		{
			try
			{
				SetFileReadOnly(filePath, false);

				EraseFile(filePath);
			}
			catch (Exception ex)
			{
				failures.Add(ex);
			}
		}

		try
		{
			Directory.Delete(directoryPath, recursive: true);
		}
		catch (Exception ex)
		{
			failures.Add(ex);
		}

		if (failures.Count > 0)
		{
			throw new AggregateException(failures);
		}
	}

	/// <inheritdoc />
	public void EraseAndDeleteFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passCount = IFileSystem.DefaultPassCount)
	{
		EraseFile(filePath, bufferSize, passCount);

		File.Delete(filePath);
	}

	/// <inheritdoc />
	public void EraseFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passCount = IFileSystem.DefaultPassCount)
	{
		long fileLength = new FileInfo(filePath).Length;

		// A buffer of the erase size is rented to keep it out of the large object heap.
		byte[] buffer = ArrayPool<byte>
			.Shared
			.Rent((int)Math.Min(fileLength, bufferSize));

		try
		{
			for (int i = 0; i < passCount; i++)
			{
				using FileStream stream = File.Open(
					filePath,
					FileMode.Open,
					FileAccess.Write);

				long position = 0;

				while (position < fileLength)
				{
					int count = (int)Math.Min(buffer.Length, fileLength - position);

					RandomNumberGenerator.Fill(buffer.AsSpan(0, count));

					stream.Write(buffer, 0, count);

					position += count;
				}

				// Without this the pass may never reach the medium: deleting the file afterwards
				// lets the system drop the pending writes and leave the original content in place.
				stream.Flush(flushToDisk: true);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	/// <inheritdoc />
	public bool FileExists([NotNullWhen(true)] string? filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
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

			return Directory
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
			using FileStream stream = File.Open(
				filePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.None);
		}
		catch (IOException ex) when (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (ex.HResult & 0x0000FFFF) == 32)
		{
			return true;
		}
		catch (IOException) when (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
	public Stream OpenRead(string filePath) => File.Open(
		filePath,
		FileMode.Open,
		FileAccess.Read,
		FileShare.ReadWrite);

	/// <inheritdoc />
	public Stream OpenSequentialRead(string filePath) => new FileStream(
		filePath,
		FileMode.Open,
		FileAccess.Read,
		FileShare.Read,
		bufferSize: 81920,
		options: FileOptions.Asynchronous | FileOptions.SequentialScan);

	/// <inheritdoc />
	public Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken token = default)
	{
		return File.ReadAllBytesAsync(filePath, token);
	}

	/// <inheritdoc />
	public string ReadAllText(string filePath) => File.ReadAllText(filePath);

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
			Directory.CreateDirectory(parentDirectory);
		}

		File.WriteAllText(filePath, _jsonSerializer.Serialize(value, JsonDefaults.Options));

		if (!isHidden)
		{
			return;
		}

		SetFileHidden(filePath, true);
	}

	/// <inheritdoc />
	public void SetFileHidden(string filePath, bool isHidden)
	{
		const FileAttributes attribute = FileAttributes.Hidden;

		if (isHidden)
		{
			AddFileAttributes(filePath, attribute);
		}
		else
		{
			RemoveFileAttributes(filePath, attribute);
		}
	}

	/// <inheritdoc />
	public void SetFileReadOnly(string filePath, bool isReadOnly)
	{
		const FileAttributes attribute = FileAttributes.ReadOnly;

		if (isReadOnly)
		{
			AddFileAttributes(filePath, attribute);
		}
		else
		{
			RemoveFileAttributes(filePath, attribute);
		}
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
		return File.WriteAllBytesAsync(filePath, bytes, token);
	}

	/// <inheritdoc />
	public async Task WriteAllBytesAtomicAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default)
	{
		string temporaryFilePath = filePath + TemporaryFileExtension;

		try
		{
			// The bytes reach the disk before the swap: a rename that outruns them publishes an empty file.
			await using (FileStream stream = new(
				temporaryFilePath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 4096,
				options: FileOptions.Asynchronous | FileOptions.WriteThrough))
			{
				await stream
					.WriteAsync(bytes, token)
					.ConfigureAwait(false);
			}

			File.Move(temporaryFilePath, filePath, overwrite: true);
		}
		catch
		{
			TryDeleteTemporaryFile(temporaryFilePath);

			throw;
		}
	}

	/// <inheritdoc />
	public void WriteAllText(string filePath, string? contents) => File.WriteAllText(filePath, contents);
	#endregion

	#region Helpers
	/// <summary>
	/// Adds attributes <see cref="FileAttributes" /> to a file.
	/// </summary>
	private static void AddFileAttributes(string filePath, FileAttributes attributes)
	{
		FileAttributes current = File.GetAttributes(filePath);

		current |= attributes;

		File.SetAttributes(filePath, current);
	}

	/// <summary>
	/// Removes the <see cref="FileAttributes" /> attribute from a file.
	/// </summary>
	private static void RemoveFileAttributes(string filePath, FileAttributes attributes)
	{
		FileAttributes current = File.GetAttributes(filePath);

		current &= ~attributes;

		File.SetAttributes(filePath, current);
	}

	/// <summary>
	/// Removes the file an atomic write was prepared in.
	/// </summary>
	private static void TryDeleteTemporaryFile(string temporaryFilePath)
	{
		try
		{
			File.Delete(temporaryFilePath);
		}
		catch
		{
			// The write is reported as it failed; a leftover file must not replace that reason.
		}
	}
	#endregion
}
