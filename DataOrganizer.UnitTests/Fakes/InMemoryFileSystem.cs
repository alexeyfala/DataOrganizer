using Serilog;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Fakes;

/// <summary>
/// Minimal in-memory <see cref="IFileSystem" /> for unit tests. Only the byte
/// read / write / erase / exists surface is implemented; unused members throw.
/// </summary>
internal sealed class InMemoryFileSystem : IFileSystem
{
	#region Properties
	/// <summary>
	/// Paths written through the atomic write, in the order they were written.
	/// </summary>
	public List<string> AtomicWrites { get; } = [];

	/// <summary>
	/// Backing store: file path to its bytes.
	/// </summary>
	public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);
	#endregion

	#region Methods
	/// <inheritdoc />
	public void CreateDirectory(string directoryPath)
	{
	}

	/// <inheritdoc />
	public void DeleteDirectory(string directoryPath, bool recursive = true)
	{
		foreach (string path in Files.Keys.Where(key => Path.GetDirectoryName(key) == directoryPath).ToArray())
		{
			Files.Remove(path);
		}
	}

	/// <inheritdoc />
	public bool DirectoryExists(string? directoryPath)
	{
		return directoryPath is not null && Files.Keys.Any(key => Path.GetDirectoryName(key) == directoryPath);
	}

	/// <inheritdoc />
	public IEnumerable<string> EnumerateFiles(string directoryPath)
	{
		return [.. Files
			.Keys
			.Where(key => Path.GetDirectoryName(key) == directoryPath)];
	}

	/// <inheritdoc />
	public void EraseAndDeleteFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passCount = IFileSystem.DefaultPassCount)
	{
		Files.Remove(filePath);
	}

	/// <inheritdoc />
	public bool FileExists(string? filePath) => filePath is not null && Files.ContainsKey(filePath);

	/// <inheritdoc />
	public byte[] ReadAllBytes(string filePath) => [.. Files[filePath]];

	/// <inheritdoc />
	public Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken token = default)
	{
		// A read hands out bytes of its own, so a caller wiping them leaves the file as it was.
		return Task.FromResult<byte[]>([.. Files[filePath]]);
	}

	/// <inheritdoc />
	public Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default)
	{
		// A write copies the bytes, so a caller wiping its buffer afterwards cannot change the file.
		Files[filePath] = [.. bytes];

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public void WriteAllBytesAtomic(string filePath, byte[] bytes)
	{
		AtomicWrites.Add(filePath);

		Files[filePath] = [.. bytes];
	}

	/// <inheritdoc />
	/// <remarks>
	/// The temporary file of the real implementation leaves no trace here: only its outcome is modelled.
	/// </remarks>
	public Task WriteAllBytesAtomicAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default)
	{
		AtomicWrites.Add(filePath);

		Files[filePath] = [.. bytes];

		return Task.CompletedTask;
	}
	#endregion

	#region Unused
	/// <inheritdoc />
	public ValueTask<byte[]> ComputeStreamHashAsync(
		HashAlgorithmName algorithm,
		Stream stream,
		CancellationToken token = default) => throw new NotSupportedException();

	/// <inheritdoc />
	public Stream CreateSequentialWrite(string filePath) => throw new NotSupportedException();

	/// <inheritdoc />
	public void DeleteDirectoryRecursively(string directoryPath, bool removeFileReadonlySign = false) => throw new NotSupportedException();

	/// <inheritdoc />
	public void EraseAndDeleteDirectory(string directoryPath) => throw new NotSupportedException();

	/// <inheritdoc />
	public void EraseFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passCount = IFileSystem.DefaultPassCount) => throw new NotSupportedException();

	/// <inheritdoc />
	public bool IsFileLocked(string filePath) => throw new NotSupportedException();

	/// <inheritdoc />
	public Stream OpenRead(string filePath) => throw new NotSupportedException();

	/// <inheritdoc />
	public Stream OpenSequentialRead(string filePath) => throw new NotSupportedException();

	/// <inheritdoc />
	public string ReadAllText(string filePath) => throw new NotSupportedException();

	/// <inheritdoc />
	public void SerializeToJsonFile<T>(T value, string filePath, bool isHidden) => throw new NotSupportedException();

	/// <inheritdoc />
	public void SetFileHidden(string filePath, bool isHidden) => throw new NotSupportedException();

	/// <inheritdoc />
	public void SetFileReadOnly(string filePath, bool isReadOnly) => throw new NotSupportedException();

	/// <inheritdoc />
	public ValueTask<bool> WaitUntilFileUnlockedAsync(
		string filePath,
		ILogger? logger = null,
		CancellationToken token = default) => throw new NotSupportedException();

	/// <inheritdoc />
	public void WriteAllText(string filePath, string? contents) => throw new NotSupportedException();
	#endregion
}
