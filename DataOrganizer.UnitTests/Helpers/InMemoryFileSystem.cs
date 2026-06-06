using Serilog;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Helpers;

/// <summary>
/// Minimal in-memory <see cref="IFileSystem" /> for unit tests. Only the byte
/// read / write / erase / exists surface is implemented; unused members throw.
/// </summary>
internal sealed class InMemoryFileSystem : IFileSystem
{
	#region Properties
	/// <summary>
	/// Backing store: file path to its bytes.
	/// </summary>
	public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);
	#endregion

	#region Methods
	public void CreateDirectory(string directoryPath)
	{
	}

	public void DeleteDirectory(string directoryPath, bool recursive = true)
	{
		foreach (string path in Files.Keys.Where(key => Path.GetDirectoryName(key) == directoryPath).ToArray())
		{
			Files.Remove(path);
		}
	}

	public void EraseAndDeleteFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passes = IFileSystem.DefaultPassCount)
	{
		Files.Remove(filePath);
	}

	public bool IsDirectoryExists(string? directoryPath)
	{
		return directoryPath is not null && Files.Keys.Any(key => Path.GetDirectoryName(key) == directoryPath);
	}

	public bool IsFileExists(string? filePath) => filePath is not null && Files.ContainsKey(filePath);

	public Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken token = default)
	{
		return Task.FromResult(Files[filePath]);
	}

	public Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default)
	{
		Files[filePath] = bytes;

		return Task.CompletedTask;
	}
	#endregion

	#region Unused
	public ValueTask<byte[]> ComputeStreamHashAsync(
		HashAlgorithmName algorithm,
		Stream stream,
		CancellationToken token = default) => throw new NotSupportedException();

	public Stream CreateSequentialWrite(string filePath) => throw new NotSupportedException();

	public void DeleteDirectoryRecursively(string directoryPath, bool removeFileReadonlySign = false) => throw new NotSupportedException();

	public void EraseFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passes = IFileSystem.DefaultPassCount) => throw new NotSupportedException();

	public bool IsFileLocked(string filePath) => throw new NotSupportedException();

	public Stream OpenRead(string filePath) => throw new NotSupportedException();

	public Stream OpenSequentialRead(string filePath) => throw new NotSupportedException();

	public string ReadAllText(string filePath) => throw new NotSupportedException();

	public void SerializeToJsonFile<T>(T value, string filePath, bool isHide) => throw new NotSupportedException();

	public void SetFileHidden(string filePath, bool value) => throw new NotSupportedException();

	public void SetFileReadOnly(string filePath, bool value) => throw new NotSupportedException();

	public Task<bool> WaitFileUnlockedAsync(
		string filePath,
		ILogger? logger = null,
		CancellationToken token = default) => throw new NotSupportedException();

	public void WriteAllText(string filePath, string? contents) => throw new NotSupportedException();
	#endregion
}
