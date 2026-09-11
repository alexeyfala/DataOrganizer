using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Interfaces;

/// <summary>
/// Wrapper over file system handling methods in <see cref="System.IO" />.
/// </summary>
public interface IFileSystem
{
	#region Data
	/// <summary>
	/// Default buffer size for <see cref="EraseFile" />.
	/// </summary>
	public const int DefaultBufferSize = 4096;

	/// <summary>
	/// Default pass count for <see cref="EraseFile" />.
	/// </summary>
	/// <remarks>
	/// A single pass: multiple passes are an HDD legacy, and on an SSD with wear leveling a rewrite
	/// is not guaranteed to reach the same physical blocks anyway.
	/// </remarks>
	public const int DefaultPassCount = 1;
	#endregion

	#region Methods
	/// <summary>
	/// Computes a hash of <see cref="Stream" /> content.
	/// </summary>
	ValueTask<byte[]> ComputeStreamHashAsync(
		HashAlgorithmName algorithm,
		Stream stream,
		CancellationToken token = default);

	/// <inheritdoc cref="Directory.CreateDirectory(string)" />
	void CreateDirectory(string directoryPath);

	/// <summary>
	/// Creates or overwrites a file for sequential, asynchronous writing.
	/// </summary>
	Stream CreateSequentialWrite(string filePath);

	/// <inheritdoc cref="Directory.Delete(string, bool)" />
	void DeleteDirectory(string directoryPath, bool recursive = true);

	/// <summary>
	/// Determines whether a folder exists in the file system, taking into account the case of the path.
	/// </summary>
	/// <remarks>
	/// The case-sensitivity of the path parameter corresponds to that of the file system on which the code is running.
	/// For example, it's case-insensitive on NTFS (the default Windows file system) and case-sensitive on Linux file systems.
	/// </remarks>
	bool DirectoryExists([NotNullWhen(true)] string? directoryPath);

	/// <inheritdoc cref="Directory.EnumerateFiles(string)" />
	IEnumerable<string> EnumerateFiles(string directoryPath);

	/// <summary>
	/// Overwrites with random values the contents of every file of the folder and of its subfolders, then deletes the folder.
	/// </summary>
	/// <remarks>
	/// The <see cref="FileAttributes.ReadOnly" /> sign is removed from every file.
	/// A file that cannot be overwritten does not stop the others; the failures are reported through <see cref="AggregateException" />.
	/// </remarks>
	void EraseAndDeleteDirectory(string directoryPath);

	/// <summary>
	/// <inheritdoc cref="EraseFile" /><br />
	/// <inheritdoc cref="File.Delete(string)" />
	/// </summary>
	void EraseAndDeleteFile(
		string filePath,
		in int bufferSize = DefaultBufferSize,
		in int passCount = DefaultPassCount);

	/// <summary>
	/// Overwrites the file contents with random values.
	/// </summary>
	void EraseFile(
		string filePath,
		in int bufferSize = DefaultBufferSize,
		in int passCount = DefaultPassCount);

	/// <summary>
	/// Determines whether a file exists in the file system, taking into account the case of the path.
	/// </summary>
	/// <remarks>
	/// The case-sensitivity of the path parameter corresponds to that of the file system on which the code is running. For example, it's case-insensitive on NTFS (the default Windows file system) and case-sensitive on Linux file systems.
	/// </remarks>
	bool FileExists([NotNullWhen(true)] string? filePath);

	/// <summary>
	/// <c>True</c> when the file is locked by another process. <br />
	/// <see href="https://code-maze.com/csharp-how-to-check-if-a-file-is-in-use" />
	/// </summary>
	bool IsFileLocked(string filePath);

	/// <summary>
	/// Opens an existing file for reading with <see cref="FileShare.ReadWrite" />, allowing concurrent
	/// writers (e.g. the user's editor) to modify the file while it is being observed.
	/// </summary>
	/// <returns>
	/// A <see cref="Stream" /> that <b>must support seeking</b> (<see cref="Stream.CanSeek" /> is
	/// <c>True</c>). Callers may rely on <see cref="Stream.Position" />, <see cref="Stream.Seek" /> and
	/// <see cref="Stream.Length" /> — for example, the file-change tracker resets the position after
	/// computing the hash to re-read the contents. Implementations that cannot guarantee seekability
	/// must not expose them via this method; use a separate non-seekable API instead.
	/// </returns>
	Stream OpenRead(string filePath);

	/// <summary>
	/// Opens an existing file for sequential, asynchronous reading.
	/// </summary>
	Stream OpenSequentialRead(string filePath);

	/// <inheritdoc cref="File.ReadAllBytesAsync(string, CancellationToken)" />
	Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken token = default);

	/// <inheritdoc cref="File.ReadAllText(string)" />
	string ReadAllText(string filePath);

	/// <summary>
	/// Serializes an object into a Json string, saving it to a file using <see cref="System.Text.Json" />.
	/// </summary>
	void SerializeToJsonFile<T>(T value, string filePath, bool hide);

	/// <summary>
	/// Adds/removes the <see cref="FileAttributes.Hidden" /> attribute to a file.
	/// </summary>
	void SetFileHidden(string filePath, bool value);

	/// <summary>
	/// Adds/removes the <see cref="FileAttributes.ReadOnly" /> attribute to a file.
	/// </summary>
	void SetFileReadOnly(string filePath, bool value);

	/// <summary>
	/// Waits until <paramref name="filePath" /> is no longer locked by another
	/// process (or no longer exists). Returns <c>True</c> in that case, or
	/// <c>False</c> if <paramref name="token" /> was cancelled while the file
	/// was still locked. Never throws on cancellation.
	/// </summary>
	ValueTask<bool> WaitUntilFileUnlockedAsync(
		string filePath,
		ILogger? logger = null,
		CancellationToken token = default);

	/// <inheritdoc cref="File.WriteAllBytesAsync(string, byte[], CancellationToken)" />
	Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default);

	/// <summary>
	/// Writes bytes into a temporary file and puts it in the place of <paramref name="filePath" />,
	/// so a write that does not reach the disk leaves the previous contents untouched.
	/// </summary>
	Task WriteAllBytesAtomicAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default);

	/// <inheritdoc cref="File.WriteAllText(string, string?)" />
	void WriteAllText(string filePath, string? contents);
	#endregion
}
