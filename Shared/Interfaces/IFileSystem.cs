using Serilog;
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
	public const int DefaultPassCount = 3;
	#endregion

	#region Methods
	/// <summary>
	/// Computes the <see cref="HashAlgorithmName.SHA256" /> hash of a file.
	/// </summary>
	byte[] ComputeSha256Hash(string filePath);

	/// <inheritdoc cref="Directory.CreateDirectory(string)" />
	void CreateDirectory(string directoryPath);

	/// <inheritdoc cref="Directory.Delete(string, bool)" />
	void DeleteDirectory(string directoryPath, bool recursive = true);

	/// <summary>
	/// <inheritdoc cref="Directory.Delete(string, bool)" />
	/// </summary>
	/// <remarks>
	/// If <paramref name="removeFileReadonlySign"/> is <c>True</c>, removes preliminarily the sign <see cref="FileAttributes.ReadOnly" /> for all files.
	/// </remarks>
	void DeleteDirectoryRecursively(
		string directoryPath,
		bool removeFileReadonlySign = false);

	/// <summary>
	/// <inheritdoc cref="EraseFile" /><br />
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

	/// <summary>
	/// Determines whether a folder exists in the file system, taking into account the case of the path.
	/// </summary>
	/// <remarks>
	/// The case-sensitivity of the path parameter corresponds to that of the file system on which the code is running.
	/// For example, it's case-insensitive on NTFS (the default Windows file system) and case-sensitive on Linux file systems.
	/// </remarks>
	[return: NotNullIfNotNull(nameof(directoryPath))]
	bool IsDirectoryExists([NotNullWhen(true)] string? directoryPath);

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
