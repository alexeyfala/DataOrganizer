using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
	/// Default buffer size for <see cref="EraseFile(string, in int, in int)" />.
	/// </summary>
	public const int DefaultBufferSize = 4096;

	/// <summary>
	/// Default pass count for <see cref="EraseFile(string, in int, in int)" />.
	/// </summary>
	public const int DefaultPassCount = 3;
	#endregion

	#region Methods
	/// <inheritdoc cref="Directory.CreateDirectory(string)" />
	void CreateDirectory(string directoryPath);

	/// <inheritdoc cref="Directory.Delete(string, bool)" />
	void DeleteDirectory(string directoryPath, bool recursive = true);

	/// <inheritdoc cref="File.Delete(string)" />
	void DeleteFile(string filePath);

	/// <summary>
	/// <inheritdoc cref="EraseFile(string, in int, in int)" /><br />
	/// <inheritdoc cref="DeleteFile(string)" />
	/// </summary>
	void EraseAndDeleteFile(
		string filePath,
		in int bufferSize = DefaultBufferSize,
		in int passes = DefaultPassCount);

	/// <summary>
	/// Overwrites the file contents with random values.
	/// </summary>
	void EraseFile(
		string filePath,
		in int bufferSize = DefaultBufferSize,
		in int passes = DefaultPassCount);

	/// <inheritdoc cref="Path.GetDirectoryName(string)" />
	string? GetParentDirectory(string? absolutePath);

	/// <summary>
	/// Determines whether a file exists in the file system, taking into account the case of the path.
	/// </summary>
	/// <remarks>
	/// The case-sensitivity of the path parameter corresponds to that of the file system on which the code is running. For example, it's case-insensitive on NTFS (the default Windows file system) and case-sensitive on Linux file systems.
	/// </remarks>
	bool IsFileExists([NotNullWhen(true)] string? filePath);

	/// <summary>
	/// Returns <c>True</c> if the file is locked by another process. <br />
	/// <see href="https://code-maze.com/csharp-how-to-check-if-a-file-is-in-use" />
	/// </summary>
	bool IsFileLocked(string filePath);

	/// <inheritdoc cref="File.Open(string, FileMode, FileAccess, FileShare)" />
	FileStream OpenFile(string filePath, FileMode mode, FileAccess access, FileShare share);

	/// <inheritdoc cref="File.ReadAllBytesAsync(string, CancellationToken)" />
	Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken token = default);

	/// <summary>
	/// Serializes an object into a Json string, saving it to a file using <see cref="System.Text.Json" />.
	/// </summary>
	void SerializeToJsonFile<T>(T value, string filePath, bool isHide);

	/// <summary>
	/// Adds/removes the <see cref="FileAttributes.Hidden" /> attribute to a file.
	/// </summary>
	void SetFileHidden(string filePath, bool value);

	/// <summary>
	/// Adds/removes the <see cref="FileAttributes.ReadOnly" /> attribute to a file.
	/// </summary>
	void SetFileReadOnly(string filePath, bool value);

	/// <summary>
	/// Checks if a file is locked by another process and waits for it to be unlocked.
	/// </summary>
	Task WaitWhileFileIsLockedAsync(
		string filePath,
		ILogger? logger = null,
		CancellationToken token = default);

	/// <inheritdoc cref="File.WriteAllBytesAsync(string, byte[], CancellationToken)" />
	Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default);
	#endregion
}
