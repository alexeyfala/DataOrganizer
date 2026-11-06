using Shared.Extensions;
using Shared.Interfaces;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using LibraryFileSystem = System.IO.Abstractions.IFileSystem;

namespace Shared.Services;

/// <inheritdoc cref="IFileSystem" />
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
	#endregion

	#region Constructors
	public FileSystem(LibraryFileSystem fileSystem) => _fileSystem = fileSystem;
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
	public bool DirectoryExists([NotNullWhen(true)] string? directoryPath) => _fileSystem.DirectoryExists(directoryPath);

	/// <inheritdoc />
	public IEnumerable<string> EnumerateFiles(string directoryPath)
	{
		return _fileSystem
			.Directory
			.EnumerateFiles(directoryPath);
	}

	/// <inheritdoc />
	public bool FileExists([NotNullWhen(true)] string? filePath) => _fileSystem.FileExists(filePath);

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
