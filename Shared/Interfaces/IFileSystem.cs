using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Interfaces;

/// <summary>
/// Wrapper over file system handling methods in <see cref="System.IO" />.
/// </summary>
public interface IFileSystem
{
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
	/// Determines whether a file exists in the file system, taking into account the case of the path.
	/// </summary>
	/// <remarks>
	/// The case-sensitivity of the path parameter corresponds to that of the file system on which the code is running. For example, it's case-insensitive on NTFS (the default Windows file system) and case-sensitive on Linux file systems.
	/// </remarks>
	bool FileExists([NotNullWhen(true)] string? filePath);

	/// <summary>
	/// Opens an existing file for reading with <see cref="FileShare.ReadWrite" /> and <see cref="FileShare.Delete" />,
	/// allowing concurrent writers (e.g. the user's editor) to modify, replace or delete the file while it is being observed.
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

	/// <inheritdoc cref="File.ReadAllBytes(string)" />
	byte[] ReadAllBytes(string filePath);

	/// <inheritdoc cref="File.ReadAllBytesAsync(string, CancellationToken)" />
	Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken token = default);

	/// <inheritdoc cref="File.ReadAllText(string)" />
	string ReadAllText(string filePath);

	/// <summary>
	/// Adds/removes the <see cref="FileAttributes.ReadOnly" /> attribute to a file.
	/// </summary>
	void SetFileReadOnly(string filePath, bool isReadOnly);

	/// <inheritdoc cref="File.SetUnixFileMode(string, UnixFileMode)" />
	[UnsupportedOSPlatform("windows")]
	void SetUnixFileMode(string path, UnixFileMode mode);

	/// <inheritdoc cref="File.WriteAllBytesAsync(string, byte[], CancellationToken)" />
	Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default);

	/// <inheritdoc cref="File.WriteAllText(string, string?)" />
	void WriteAllText(string filePath, string? contents);
	#endregion
}
