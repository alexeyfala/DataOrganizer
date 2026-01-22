using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Services;

public sealed class FileSystem : IFileSystem
{
	#region Data
	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;
	#endregion

	#region Constructors
	public FileSystem(IJsonSerializerWrapper jsonSerializer)
	{
		_jsonSerializer = jsonSerializer;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);

	/// <inheritdoc />
	public void DeleteDirectory(string directoryPath, bool recursive = true)
	{
		Directory.Delete(directoryPath, recursive);
	}

	/// <inheritdoc />
	public void DeleteFile(string filePath) => File.Delete(filePath);

	/// <inheritdoc />
	public void EraseAndDeleteFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passes = IFileSystem.DefaultPassCount)
	{
		EraseFile(filePath, bufferSize, passes);

		DeleteFile(filePath);
	}

	/// <inheritdoc />
	public void EraseFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passes = IFileSystem.DefaultPassCount)
	{
		using RandomNumberGenerator generator = RandomNumberGenerator.Create();

		byte[] buffer = new byte[bufferSize];

		long fileLength = new FileInfo(filePath).Length;

		for (int i = 0; i < passes; i++)
		{
			using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Write);

			long position = 0;

			while (position < fileLength)
			{
				generator.GetBytes(buffer);

				stream.Write(buffer, 0, (int)Math.Min(buffer.Length, fileLength - position));

				position += buffer.Length;
			}
		}
	}

	/// <inheritdoc />
	public string? GetParentDirectory(string? absolutePath) => Path.GetDirectoryName(absolutePath);

	/// <inheritdoc />
	public bool IsFileExists([NotNullWhen(true)] string? filePath)
	{
		if (!File.Exists(filePath) || GetParentDirectory(filePath) is not { } parent)
		{
			return false;
		}

		try
		{
			return Array.Exists(Directory.GetFiles(parent), x => x == Path.GetFullPath(filePath));
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
			using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
		}
		catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32)
		{
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public FileStream OpenFile(
		string filePath,
		FileMode mode,
		FileAccess access,
		FileShare share)
	{
		return File.Open(filePath, mode, access, share);
	}

	/// <inheritdoc />
	public Task<byte[]> ReadAllBytesAsync(
		string filePath,
		CancellationToken token = default)
	{
		return File.ReadAllBytesAsync(filePath, token);
	}

	/// <inheritdoc />
	public void SerializeToJsonFile<T>(
		T value,
		string filePath,
		bool isHide)
	{
		if (IsFileExists(filePath))
		{
			SetFileHidden(filePath, false);
		}

		if (GetParentDirectory(filePath) is { } parentDirectory)
		{
			Directory.CreateDirectory(parentDirectory);
		}

		File.WriteAllText(filePath, _jsonSerializer.Serialize(value, AppUtils.JsonOptions));

		if (!isHide)
		{
			return;
		}

		SetFileHidden(filePath, true);
	}

	/// <inheritdoc />
	public void SetFileHidden(string filePath, bool value)
	{
		const FileAttributes attribute = FileAttributes.Hidden;

		if (value)
		{
			AddFileAttributes(filePath, attribute);
		}
		else
		{
			RemoveFileAttributes(filePath, attribute);
		}
	}

	/// <inheritdoc />
	public void SetFileReadOnly(string filePath, bool value)
	{
		const FileAttributes attribute = FileAttributes.ReadOnly;

		if (value)
		{
			AddFileAttributes(filePath, attribute);
		}
		else
		{
			RemoveFileAttributes(filePath, attribute);
		}
	}

	/// <inheritdoc />
	public async Task WaitWhileFileIsLockedAsync(
		string filePath,
		ILogger? logger = null,
		CancellationToken token = default)
	{
		while (IsFileExists(filePath) && IsFileLocked(filePath))
		{
			logger?.LogWarning($@"File ""{filePath}"" is locked by another process, waiting it to be released.");

			await Task.Delay(500, token).ConfigureAwait(false);
		}
	}

	/// <inheritdoc />
	public Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default)
	{
		return File.WriteAllBytesAsync(filePath, bytes, token);
	}
	#endregion

	#region Service
	/// <summary>
	/// Adds attributes <see cref="FileAttributes" /> to a file.
	/// </summary>
	private static void AddFileAttributes(string filePath, in FileAttributes value)
	{
		FileAttributes attributes = File.GetAttributes(filePath);

		attributes |= value;

		File.SetAttributes(filePath, attributes);
	}

	/// <summary>
	/// Removes the <see cref="FileAttributes" /> attribute from a file.
	/// </summary>
	private static void RemoveFileAttributes(string filePath, in FileAttributes value)
	{
		FileAttributes attributes = File.GetAttributes(filePath);

		attributes &= ~value;

		File.SetAttributes(filePath, attributes);
	}
	#endregion
}
